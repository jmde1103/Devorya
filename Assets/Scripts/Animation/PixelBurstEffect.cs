using UnityEngine;

// <변경부분> 검은 픽셀 조각 파티클을 원하는 위치에서 재생하는 공용 이펙트 컨트롤러
public class PixelBurstEffect : MonoBehaviour
{
    [Header("Particle System")]
    // <변경부분> 실제 파티클 시스템 참조
    [SerializeField] private ParticleSystem particleSystemEffect;

    [Header("Auto Destroy")]
    // <변경부분> 재생 후 자동으로 비활성화할지 여부
    [SerializeField] private bool disableAfterPlay = false;

    private void Awake()
    {
        // <변경부분> 파티클 시스템이 비어 있으면 현재 오브젝트 또는 자식에서 자동 탐색
        if (particleSystemEffect == null)
        {
            particleSystemEffect = GetComponentInChildren<ParticleSystem>();
        }
    }

    // <변경부분> 현재 위치에서 파티클 재생
    public void Play()
    {
        if (particleSystemEffect == null)
        {
            return;
        }

        particleSystemEffect.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        particleSystemEffect.Play();

        if (disableAfterPlay)
        {
            float lifeTime = particleSystemEffect.main.duration + particleSystemEffect.main.startLifetime.constantMax;
            CancelInvoke(nameof(DisableSelf));
            Invoke(nameof(DisableSelf), lifeTime);
        }
    }

    // <변경부분> 월드 위치를 지정해서 파티클 재생
    public void PlayAtPosition(Vector3 worldPosition)
    {
        transform.position = worldPosition;
        Play();
    }

    // <변경부분> 월드 위치에서 파티클을 재생한 뒤 자동으로 제거
    // 버튼 클릭처럼 순간적으로 생성되는 이펙트 프리팹용
    public void PlayAtPositionAndDestroy(Vector3 worldPosition)
    {
        transform.position = worldPosition;
        Play();

        float lifeTime = GetEffectLifeTime();
        Destroy(gameObject, lifeTime);
    }

    // <변경부분> 파티클 재생이 완전히 끝나는 시간을 계산
    private float GetEffectLifeTime()
    {
        if (particleSystemEffect == null)
        {
            return 1f;
        }

        ParticleSystem.MainModule main = particleSystemEffect.main;

        return main.duration
            + main.startDelay.constantMax
            + main.startLifetime.constantMax
            + 0.1f;
    }

    // <변경부분> 특정 타겟 위치에서 파티클 재생
    public void PlayAtTransform(Transform target)
    {
        if (target == null)
        {
            return;
        }

        transform.position = target.position;
        Play();
    }

    // <변경부분> 재생 후 자동 비활성화
    private void DisableSelf()
    {
        gameObject.SetActive(false);
    }
}