using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Transforms;
using UnityEngine;

/// <summary>
/// VAT Entity 스폰 예시
/// - Authoring 컴포넌트로 Baker를 통해 Entity 변환
/// - 또는 런타임에서 직접 스폰
/// </summary>
public class VATSpawner : MonoBehaviour
{
    [Header("VAT Settings")]
    public Mesh     VATMesh;
    public Material VATMaterial;        // VAT Shader Graph 머티리얼

    [Header("Animation Settings")]
    public float    FrameCount  = 60;
    public float    Fps         = 30;
    public bool     IsLooping   = true;

    [Header("Spawn Settings")]
    public int      SpawnCount  = 1000;
    public float    SpawnRadius = 20f;

    private void Start()
    {
        var world  = World.DefaultGameObjectInjectionWorld;
        var em     = world.EntityManager;

        // 렌더링 디스크립터
        var renderDesc = new RenderMeshDescription(
            shadowCastingMode: UnityEngine.Rendering.ShadowCastingMode.On,
            receiveShadows: true);

        var renderMeshArray = new RenderMeshArray(
            new[] { VATMaterial },
            new[] { VATMesh });

        // 아키타입 생성
        var archetype = em.CreateArchetype(
            typeof(LocalTransform),
            typeof(LocalToWorld),
            typeof(VATAnimationData),
            typeof(VATCurrentFrameProperty));

        // 엔티티 배치 스폰
        using var entities = em.CreateEntity(archetype, SpawnCount, Unity.Collections.Allocator.Temp);

        var random = new Unity.Mathematics.Random(12345u);

        for (int i = 0; i < SpawnCount; i++)
        {
            var entity = entities[i];

            // 랜덤 위치
            float2 circle  = random.NextFloat2Direction() * random.NextFloat(0f, SpawnRadius);
            float3 position = new float3(circle.x, 0f, circle.y);

            em.SetComponentData(entity, LocalTransform.FromPosition(position));

            // 애니메이션 컴포넌트 (프레임 오프셋으로 동기화 방지)
            em.SetComponentData(entity, new VATAnimationData
            {
                CurrentFrame  = random.NextFloat(0f, FrameCount), // 랜덤 오프셋
                FrameCount    = FrameCount,
                Fps           = Fps,
                PlaybackSpeed = 1f,
                IsLooping     = IsLooping
            });

            em.SetComponentData(entity, new VATCurrentFrameProperty { Value = 0f });

            // 렌더 메시 설정
            RenderMeshUtility.AddComponents(entity, em, renderDesc, renderMeshArray,
                MaterialMeshInfo.FromRenderMeshArrayIndices(0, 0));
        }

        Debug.Log($"[VATSpawner] {SpawnCount}개 엔티티 스폰 완료");
    }
}
