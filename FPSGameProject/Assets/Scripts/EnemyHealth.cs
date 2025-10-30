using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyHealth : MonoBehaviour
{
    public float maxHealth = 100f;
    public float currentHealth;

    public GameObject deathEffect;
    public GameObject damageEffect;
    public float destroyDelay = 0.4f;

    public GameObject player;

    public void AttackDamage()
    {
        float distance = Vector3.Distance(transform.position,player.transform.position);
        if(distance < 3f )
        {
            //PlayerController.Damaged(5); -> 플레이어 대미지 주기

        }
    }
}
