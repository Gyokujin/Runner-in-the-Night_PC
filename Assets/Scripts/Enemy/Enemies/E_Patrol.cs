using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class E_Patrol : Enemy
{
    [Header("Move")]
    [SerializeField]
    private bool onGround = true; // ����, ���� ���͸� ����
    private bool onMove = false;
    [SerializeField]
    private Transform[] landVec; // 0 : ����, 1 : ������
    [SerializeField]
    private float groundDis; // ���� �˻縦 �� �� ��� ������ ����
    [SerializeField]
    private float patrolTime;
    
    [Header("Detector")]
    private bool onDetect = false;

    [Header("Attack")]
    [SerializeField]
    private float patternDelay;
    [SerializeField]
    private float attackDelay;
    [SerializeField]
    private float shootSpeed;
    private bool onAttack;
    [SerializeField]
    private Transform emitter;
    [SerializeField]
    private GameObject bullet;

    void Start()
    {
        Think();
    }

    void Think()
    {
        if (onDetect)
        {
            animator.SetBool("onDetect", true);
        }
        else
        {
            animator.SetBool("onDetect", false);
            int pattern = Random.Range(0, 5);

            switch (pattern)
            {
                case 0:
                case 1:
                    Patrol(Vector2.left);
                    break;

                case 2:
                    Invoke("Think", patternDelay);
                    break;

                case 3:
                case 4:
                    Patrol(Vector2.right);
                    break;
            }
        }
    }

    void Patrol(Vector2 dir)
    {
        onMove = true;
        moveVec = dir;
        sprite.flipX = dir == Vector2.left ? false : true; // ������ ������ flipX�� ��Ȱ��ȭ �������� Ȱ��ȭ�Ѵ�.
        animator.SetBool("onMove", true);
        StartCoroutine("PatrolProcess");
    }

    IEnumerator PatrolProcess()
    {
        float time = patrolTime;
        rigid.velocity = moveVec * moveSpeed;

        while (time > 0 && GroundCheck())
        {
            time -= Time.deltaTime;
            yield return null;
        }

        onMove = false;
        animator.SetBool("onMove", false);
        rigid.velocity = Vector2.zero;

        new WaitForSeconds(patternDelay);
        Think();
    }

    bool GroundCheck()
    {
        Vector2 start = rigid.position;
        Vector2 dis = rigid.velocity.x < 0 ? landVec[0].localPosition : landVec[1].localPosition; // �� : landVec[0], �� : landVec[1]
        start += dis; 

        Debug.DrawRay(start, Vector2.down * groundDis, Color.green);
        RaycastHit2D platCheck = Physics2D.Raycast(start, Vector2.down, groundDis, LayerMask.GetMask("Ground"));
        return platCheck;
    }

    public void Detect(Vector2 target)
    {
        if (onAttack)
            return;

        if (!onDetect)
        {
            onDetect = true;
            AudioManager.instance.PlaySystemSFX(AudioManager.SystemSFX.Detect);
        }

        rigid.velocity = Vector2.zero;
        Vector2 targetPos = target;
        sprite.flipX = false; // �������θ� ���
        animator.SetBool("onMove", false);
        animator.SetBool("onDetect", true);
        StartCoroutine("Attack", targetPos);
    }

    IEnumerator Attack(Vector2 pos)
    {
        yield return new WaitForSeconds(attackDelay);

        if (onDie || transform.position.x < pos.x)
            yield break;

        animator.SetBool("onAttack", true);
        Vector2 dir = (pos - rigid.position).normalized;
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        Quaternion rotation = Quaternion.AngleAxis(angle - 180f, Vector3.forward); // ������ ������ �÷��̾�� ���ϰ� �Ѵ�.

        GameObject spawnBullet = PoolManager.instance.Get(PoolManager.PoolType.Bullet, 1);
        spawnBullet.transform.position = emitter.position;
        spawnBullet.transform.rotation = rotation;
        spawnBullet.GetComponent<Bullet>().Shoot(dir, shootSpeed);

        switch (kind)
        {
            case EnemyKind.Blazer:
                AudioManager.instance.PlayEnemySFX(AudioManager.EnemySfx.BlazerAttack);
                break;
        }

        yield return new WaitForSeconds(patternDelay); // ���� �ְ� �����͸� Ȱ��ȭ
        animator.SetBool("onAttack", false);
        onAttack = false;
        detector.SetActive(true);
        Think();
    }
}