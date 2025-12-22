using UnityEngine;

// 간단한 액터 베이스 클래스
public class Actor : MonoBehaviour
{
    public bool canUpdate = true;

    protected virtual void Update()
    {
        if (CanAct())
        {
            ActorUpdate();
        }
    }

    protected virtual void FixedUpdate()
    {
        if (CanAct())
        {
            ActorFixedUpdate();
        }
    }

    // 액터가 동작할 수 있는지 확인
    protected bool CanAct()
    {
        if (!canUpdate) return false;
        if (GameManager.Instance == null) return true;

        GameState state = GameManager.Instance.currentGameState;
        return state == GameState.Playing;
    }

    // 상속받아서 구현할 메소드들
    protected virtual void ActorUpdate() { }
    protected virtual void ActorFixedUpdate() { }
}