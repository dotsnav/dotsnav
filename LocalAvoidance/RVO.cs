/*
 * RVO2 Library C#
 *
 * Copyright 2008 University of North Carolina at Chapel Hill
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 *
 * Please send all bug reports to <geom@cs.unc.edu>.
 *
 * The authors may be contacted via:
 *
 * Jur van den Berg, Stephen J. Guy, Jamie Snape, Ming C. Lin, Dinesh Manocha
 * Dept. of Computer Science
 * 201 S. Columbia St.
 * Frederick P. Brooks, Jr. Computer Science Bldg.
 * Chapel Hill, N.C. 27599-3175
 * United States of America
 *
 * <http://gamma.cs.unc.edu/RVO2/>
 */

using DotsNav.LocalAvoidance.Data;
using Unity.Collections;
using Unity.Mathematics;

namespace DotsNav.LocalAvoidance
{
    static unsafe class RVO
    {
        const float Epsilon = 0.00001f;

        public static float2 CalculateNewVelocity(AgentComponent agent, float2 pos, float radius, NativeList<VelocityObstacle> neighbours,
                                                  NativeList<ObstacleDistance> obstacleNeighbours, float invTimeStep)
        {
            var prefVelocity = agent.PrefVelocity;
            Assert.IsTrue(prefVelocity.IsNumber());
            if (!prefVelocity.IsNumber() || math.all(prefVelocity == 0))
                return 0;

            var orcaLines = new NativeArray<Line>(3 * agent.MaxNeighbours, Allocator.Temp);
            var projLines = new NativeArray<Line>(3 * agent.MaxNeighbours, Allocator.Temp);
            var lineCount = CreateOrcaLines(pos, radius, agent.Velocity, neighbours, orcaLines, 1 / agent.TimeHorizon,
                out var numObstLines, 1 / agent.TimeHorizonObst, obstacleNeighbours, invTimeStep);
            var lineFail = LinearProgram2(orcaLines, agent.MaxSpeed, prefVelocity, false, out var newVelocity, lineCount);
            if (lineFail < lineCount)
                LinearProgram3(orcaLines, numObstLines, lineFail, agent.MaxSpeed, ref newVelocity, projLines, lineCount);
            return newVelocity;
        }

        static int CreateOrcaLines(float2 position, float radius, float2 velocity, NativeList<VelocityObstacle> neighbours,
                                   NativeArray<Line> lines, float invTimeHorizon, out int numObstLines, float invTimeHorizonObst,
                                   NativeList<ObstacleDistance> obstacles, float invTimeStep)
        {
            var lineCount = 0;

            /* Create obstacle ORCA lines. */
            for (var i = 0; i < obstacles.Length; ++i)
            {
                var obstacle1 = obstacles[i].Obstacle;
                var obstacle2 = obstacle1->Next;

                var relativePosition1 = obstacle1->Point - position;
                var relativePosition2 = obstacle2->Point - position;

                /*
                 * Check if velocity obstacle of obstacle is already taken care
                 * of by previously constructed obstacle ORCA lines.
                 */
                var alreadyCovered = false;

                for (var j = 0; j < lineCount; ++j)
                {
                    if (math.determinant(new float2x2(invTimeHorizonObst * relativePosition1 - lines[j].Point, lines[j].Direction)) - invTimeHorizonObst * radius >= -Epsilon && math.determinant(new float2x2(invTimeHorizonObst * relativePosition2 - lines[j].Point, lines[j].Direction)) - invTimeHorizonObst * radius >= -Epsilon)
                    {
                        alreadyCovered = true;

                        break;
                    }
                }

                if (alreadyCovered)
                {
                    continue;
                }

                /* Not yet covered. Check for collisions. */
                var distSq1 = math.lengthsq(relativePosition1);
                var distSq2 = math.lengthsq(relativePosition2);

                var radiusSq = Math.Square(radius);

                var obstacleVector = obstacle2->Point - obstacle1->Point;
                var s = math.dot(-relativePosition1, obstacleVector) / math.lengthsq(obstacleVector);
                var distSqLine = math.lengthsq(-relativePosition1 - s * obstacleVector);

                Line line;

                if (s < 0.0f && distSq1 <= radiusSq)
                {
                    /* Collision with left vertex. Ignore if non-convex. */
                    if (obstacle1->Convex)
                    {
                        line.Point = new float2(0.0f, 0.0f);
                        line.Direction = math.normalize(new float2(-relativePosition1.y, relativePosition1.x));
                        lines[lineCount++] = line;
                    }

                    continue;
                }
                else if (s > 1.0f && distSq2 <= radiusSq)
                {
                    /*
                     * Collision with right vertex. Ignore if non-convex or if
                     * it will be taken care of by neighboring obstacle.
                     */
                    if (obstacle2->Convex && math.determinant(new float2x2(relativePosition2, obstacle2->Direction)) >= 0.0f)
                    {
                        line.Point = new float2(0.0f, 0.0f);
                        line.Direction = math.normalize(new float2(-relativePosition2.y, relativePosition2.x));
                        lines[lineCount++] = line;
                    }

                    continue;
                }
                else if (s >= 0.0f && s < 1.0f && distSqLine <= radiusSq)
                {
                    /* Collision with obstacle segment. */
                    line.Point = new float2(0.0f, 0.0f);
                    line.Direction = -obstacle1->Direction;
                    lines[lineCount++] = line;

                    continue;
                }

                /*
                 * No collision. Compute legs. When obliquely viewed, both legs
                 * can come from a single vertex. Legs extend cut-off line when
                 * non-convex vertex.
                 */

                float2 leftLegDirection, rightLegDirection;

                if (s < 0.0f && distSqLine <= radiusSq)
                {
                    /*
                     * Obstacle viewed obliquely so that left vertex
                     * defines velocity obstacle.
                     */
                    if (!obstacle1->Convex)
                    {
                        /* Ignore obstacle. */
                        continue;
                    }

                    obstacle2 = obstacle1;

                    var leg1 = math.sqrt(distSq1 - radiusSq);
                    leftLegDirection = new float2(relativePosition1.x * leg1 - relativePosition1.y * radius, relativePosition1.x * radius + relativePosition1.y * leg1) / distSq1;
                    rightLegDirection = new float2(relativePosition1.x * leg1 + relativePosition1.y * radius, -relativePosition1.x * radius + relativePosition1.y * leg1) / distSq1;
                }
                else if (s > 1.0f && distSqLine <= radiusSq)
                {
                    /*
                     * Obstacle viewed obliquely so that
                     * right vertex defines velocity obstacle.
                     */
                    if (!obstacle2->Convex)
                    {
                        /* Ignore obstacle. */
                        continue;
                    }

                    obstacle1 = obstacle2;

                    var leg2 = math.sqrt(distSq2 - radiusSq);
                    leftLegDirection = new float2(relativePosition2.x * leg2 - relativePosition2.y * radius, relativePosition2.x * radius + relativePosition2.y * leg2) / distSq2;
                    rightLegDirection = new float2(relativePosition2.x * leg2 + relativePosition2.y * radius, -relativePosition2.x * radius + relativePosition2.y * leg2) / distSq2;
                }
                else
                {
                    /* Usual situation. */
                    if (obstacle1->Convex)
                    {
                        var leg1 = math.sqrt(distSq1 - radiusSq);
                        leftLegDirection = new float2(relativePosition1.x * leg1 - relativePosition1.y * radius, relativePosition1.x * radius + relativePosition1.y * leg1) / distSq1;
                    }
                    else
                    {
                        /* Left vertex non-convex; left leg extends cut-off line. */
                        leftLegDirection = -obstacle1->Direction;
                    }

                    if (obstacle2->Convex)
                    {
                        var leg2 = math.sqrt(distSq2 - radiusSq);
                        rightLegDirection = new float2(relativePosition2.x * leg2 + relativePosition2.y * radius, -relativePosition2.x * radius + relativePosition2.y * leg2) / distSq2;
                    }
                    else
                    {
                        /* Right vertex non-convex; right leg extends cut-off line. */
                        rightLegDirection = obstacle1->Direction;
                    }
                }

                /*
                 * Legs can never point into neighboring edge when convex
                 * vertex, take cutoff-line of neighboring edge instead. If
                 * velocity projected on "foreign" leg, no constraint is added.
                 */

                var leftNeighbor = obstacle1->Previous;

                var isLeftLegForeign = false;
                var isRightLegForeign = false;

                if (obstacle1->Convex && math.determinant(new float2x2(leftLegDirection, -leftNeighbor->Direction)) >= 0.0f)
                {
                    /* Left leg points into obstacle. */
                    leftLegDirection = -leftNeighbor->Direction;
                    isLeftLegForeign = true;
                }

                if (obstacle2->Convex && math.determinant(new float2x2(rightLegDirection, obstacle2->Direction)) <= 0.0f)
                {
                    /* Right leg points into obstacle. */
                    rightLegDirection = obstacle2->Direction;
                    isRightLegForeign = true;
                }

                /* Compute cut-off centers. */
                var leftCutOff = invTimeHorizonObst * (obstacle1->Point - position);
                var rightCutOff = invTimeHorizonObst * (obstacle2->Point - position);
                var cutOffVector = rightCutOff - leftCutOff;

                /* Project current velocity on velocity obstacle. */

                /* Check if current velocity is projected on cutoff circles. */
                var t = obstacle1 == obstacle2 ? 0.5f : math.dot((velocity - leftCutOff), cutOffVector) / math.lengthsq(cutOffVector);
                var tLeft = math.dot(velocity - leftCutOff, leftLegDirection);
                var tRight = math.dot(velocity - rightCutOff, rightLegDirection);

                if ((t < 0.0f && tLeft < 0.0f) || (obstacle1 == obstacle2 && tLeft < 0.0f && tRight < 0.0f))
                {
                    /* Project on left cut-off circle. */
                    var unitW = math.normalize(velocity - leftCutOff);

                    line.Direction = new float2(unitW.y, -unitW.x);
                    line.Point = leftCutOff + radius * invTimeHorizonObst * unitW;
                    lines[lineCount++] = line;

                    continue;
                }
                else if (t > 1.0f && tRight < 0.0f)
                {
                    /* Project on right cut-off circle. */
                    var unitW = math.normalize(velocity - rightCutOff);

                    line.Direction = new float2(unitW.y, -unitW.x);
                    line.Point = rightCutOff + radius * invTimeHorizonObst * unitW;
                    lines[lineCount++] = line;

                    continue;
                }

                /*
                 * Project on left leg, right leg, or cut-off line, whichever is
                 * closest to velocity.
                 */
                var distSqCutoff = (t < 0.0f || t > 1.0f || obstacle1 == obstacle2) ? float.PositiveInfinity : math.lengthsq(velocity - (leftCutOff + t * cutOffVector));
                var distSqLeft = tLeft < 0.0f ? float.PositiveInfinity : math.lengthsq(velocity - (leftCutOff + tLeft * leftLegDirection));
                var distSqRight = tRight < 0.0f ? float.PositiveInfinity : math.lengthsq(velocity - (rightCutOff + tRight * rightLegDirection));

                if (distSqCutoff <= distSqLeft && distSqCutoff <= distSqRight)
                {
                    /* Project on cut-off line. */
                    line.Direction = -obstacle1->Direction;
                    line.Point = leftCutOff + radius * invTimeHorizonObst * new float2(-line.Direction.y, line.Direction.x);
                    lines[lineCount++] = line;

                    continue;
                }

                if (distSqLeft <= distSqRight)
                {
                    /* Project on left leg. */
                    if (isLeftLegForeign)
                    {
                        continue;
                    }

                    line.Direction = leftLegDirection;
                    line.Point = leftCutOff + radius * invTimeHorizonObst * new float2(-line.Direction.y, line.Direction.x);
                    lines[lineCount++] = line;

                    continue;
                }

                /* Project on right leg. */
                if (isRightLegForeign)
                {
                    continue;
                }

                line.Direction = -rightLegDirection;
                line.Point = rightCutOff + radius * invTimeHorizonObst * new float2(-line.Direction.y, line.Direction.x);
                lines[lineCount++] = line;
            }

            numObstLines = lineCount;

            for (var i = 0; i < neighbours.Length; ++i)
            {
                var neighbour = neighbours[i];
                var relativePosition = neighbour.Position - position;
                var relativeVelocity = velocity - neighbour.Velocity;
                var distSq = math.lengthsq(relativePosition);
                var combinedRadius = radius + neighbour.Radius;
                var combinedRadiusSq = Math.Square(combinedRadius);

                Line line;
                float2 u;

                if (distSq > combinedRadiusSq)
                {
                    /* No collision. */
                    var w = relativeVelocity - invTimeHorizon * relativePosition;

                    /* Vector from cutoff center to relative velocity. */
                    var wLengthSq = math.lengthsq(w);
                    var dotProduct1 = math.dot(w, relativePosition);

                    if (dotProduct1 < 0.0f && Math.Square(dotProduct1) > combinedRadiusSq * wLengthSq)
                    {
                        /* Project on cut-off circle. */
                        var wLength = math.sqrt(wLengthSq);
                        var unitW = w / wLength;

                        line.Direction = new float2(unitW.y, -unitW.x);
                        u = (combinedRadius * invTimeHorizon - wLength) * unitW;
                    }
                    else
                    {
                        /* Project on legs. */
                        var leg = math.sqrt(distSq - combinedRadiusSq);

                        if (math.determinant(new float2x2(relativePosition, w)) > 0.0f)
                        {
                            /* Project on left leg. */
                            line.Direction = new float2(relativePosition.x * leg - relativePosition.y * combinedRadius, relativePosition.x * combinedRadius + relativePosition.y * leg) / distSq;
                        }
                        else
                        {
                            /* Project on right leg. */
                            line.Direction = -new float2(relativePosition.x * leg + relativePosition.y * combinedRadius, -relativePosition.x * combinedRadius + relativePosition.y * leg) / distSq;
                        }

                        var dotProduct2 = math.dot(relativeVelocity, line.Direction);
                        u = dotProduct2 * line.Direction - relativeVelocity;
                    }
                }
                else
                {
                    /* Collision. Project on cut-off circle of time timeStep. */

                    /* Vector from cutoff center to relative velocity. */
                    var w = relativeVelocity - invTimeStep * relativePosition;

                    var wLength = math.length(w);
                    var unitW = w / wLength;

                    line.Direction = new float2(unitW.y, -unitW.x);
                    u = (combinedRadius * invTimeStep - wLength) * unitW;
                }

                line.Point = velocity + 0.5f * u;
                lines[lineCount++] = line;
            }

            return lineCount;
        }

        static bool LinearProgram1(NativeArray<Line> lines, int lineNo, float radius, float2 optVelocity, bool directionOpt, ref float2 result)
        {
            var dotProduct = math.dot(lines[lineNo].Point, lines[lineNo].Direction);
            var discriminant = Math.Square(dotProduct) + Math.Square(radius) - math.lengthsq(lines[lineNo].Point);

            if (discriminant < 0.0f)
            {
                /* Max speed circle fully invalidates line lineNo. */
                return false;
            }

            var sqrtDiscriminant = math.sqrt(discriminant);
            var tLeft = -dotProduct - sqrtDiscriminant;
            var tRight = -dotProduct + sqrtDiscriminant;

            for (var i = 0; i < lineNo; ++i)
            {
                var denominator = math.determinant(new float2x2(lines[lineNo].Direction, lines[i].Direction));
                var numerator = math.determinant(new float2x2(lines[i].Direction, lines[lineNo].Point - lines[i].Point));

                if (math.abs(denominator) <= Epsilon)
                {
                    /* Lines lineNo and i are (almost) parallel. */
                    if (numerator < 0.0f)
                        return false;
                    continue;
                }

                var t = numerator / denominator;

                if (denominator >= 0.0f)
                    /* Line i bounds line lineNo on the right. */
                    tRight = math.min(tRight, t);
                else
                    /* Line i bounds line lineNo on the left. */
                    tLeft = math.max(tLeft, t);

                if (tLeft > tRight)
                    return false;
            }

            if (directionOpt)
            {
                /* Optimize direction. */
                if (math.dot(optVelocity, lines[lineNo].Direction) > 0.0f)
                    /* Take right extreme. */
                    result = lines[lineNo].Point + tRight * lines[lineNo].Direction;
                else
                    /* Take left extreme. */
                    result = lines[lineNo].Point + tLeft * lines[lineNo].Direction;
            }
            else
            {
                /* Optimize closest point. */
                var t = math.dot(lines[lineNo].Direction, optVelocity - lines[lineNo].Point);

                if (t < tLeft)
                    result = lines[lineNo].Point + tLeft * lines[lineNo].Direction;
                else if (t > tRight)
                    result = lines[lineNo].Point + tRight * lines[lineNo].Direction;
                else
                    result = lines[lineNo].Point + t * lines[lineNo].Direction;
            }

            return true;
        }

        static int LinearProgram2(NativeArray<Line> lines, float radius, float2 optVelocity, bool directionOpt, out float2 result, int lineCount)
        {
            if (directionOpt)
            {
                /*
                 * Optimize direction. Note that the optimization velocity is of
                 * unit length in this case.
                 */
                result = optVelocity * radius;
            }
            else
            {
                var lengthsq = math.lengthsq(optVelocity);

                if (lengthsq > Math.Square(radius))
                {
                    /* Optimize closest point and outside circle. */
                    result = math.rsqrt(lengthsq) * optVelocity * radius;
                }
                else
                {
                    /* Optimize closest point and inside circle. */
                    result = optVelocity;
                }
            }

            for (var i = 0; i < lineCount; ++i)
            {
                if (math.determinant(new float2x2(lines[i].Direction, lines[i].Point - result)) > 0.0f)
                {
                    /* Result does not satisfy constraint i. Compute new optimal result. */
                    var tempResult = result;

                    if (!LinearProgram1(lines, i, radius, optVelocity, directionOpt, ref result))
                    {
                        result = tempResult;
                        return i;
                    }
                }
            }

            return lineCount;
        }

        static void LinearProgram3(NativeArray<Line> lines, int numObstLines, int beginLine, float radius, ref float2 result, NativeArray<Line> projLines, int orcaLines)
        {
            var distance = 0.0f;

            for (var i = beginLine; i < orcaLines; ++i)
            {
                if (math.determinant(new float2x2(lines[i].Direction, lines[i].Point - result)) > distance)
                {
                    var lineCount = 0;
                    /* Result does not satisfy constraint of line i. */
                    for (var ii = 0; ii < numObstLines; ++ii)
                        projLines[lineCount++] = lines[ii];

                    for (var j = numObstLines; j < i; ++j)
                    {
                        Line line;
                        var determinant = math.determinant(new float2x2(lines[i].Direction, lines[j].Direction));

                        if (math.abs(determinant) <= Epsilon)
                        {
                            /* Line i and line j are parallel. */
                            if (math.dot(lines[i].Direction, lines[j].Direction) > 0.0f)
                            {
                                /* Line i and line j point in the same direction. */
                                continue;
                            }

                            /* Line i and line j point in opposite direction. */
                            line.Point = 0.5f * (lines[i].Point + lines[j].Point);
                        }
                        else
                        {
                            line.Point = lines[i].Point + math.determinant(new float2x2(lines[j].Direction, lines[i].Point - lines[j].Point)) / determinant * lines[i].Direction;
                        }

                        line.Direction = math.normalize(lines[j].Direction - lines[i].Direction);
                        projLines[lineCount++] = line;
                    }

                    var tempResult = result;
                    if (LinearProgram2(projLines, radius, new float2(-lines[i].Direction.y, lines[i].Direction.x), true, out result, lineCount) < lineCount)
                    {
                        /*
                         * This should in principle not happen. The result is by
                         * definition already in the feasible region of this
                         * linear program. If it fails, it is due to small
                         * floating point error, and the current result is kept.
                         */
                        result = tempResult;
                    }

                    distance = math.determinant(new float2x2(lines[i].Direction, lines[i].Point - result));
                }
            }
        }
    }
}