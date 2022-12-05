using DotsNav.Data;
using Unity.Entities;
using Unity.Transforms;

namespace DotsNav.Hybrid
{
    public class DotsNavAgent : ToEntity
    {
        public DotsNavPlane Plane;
        public float Radius = .5f;

        protected override void Convert(EntityManager entityManager, Entity entity)
        {
            Assert.IsTrue(Radius > 0, "Radius must be larger than 0");
            entityManager.AddComponentData(entity, new RadiusComponent {Value = Radius});
            entityManager.AddComponentObject(entity, this);

            entityManager.AddComponent<LocalTransform>(entity);
        }
    }
}