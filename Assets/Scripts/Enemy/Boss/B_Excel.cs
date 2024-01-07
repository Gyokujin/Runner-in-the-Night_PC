using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class B_Excel : MonoBehaviour
{
    public enum Phase
    {
        Phase1,
        Phase2,
        Phase3,
    }

    public Phase phase; // 1 ~ 3��������� ������ ���� ������ ����� ���� �����Ѵ�.
    [SerializeField]
    private int[] phaseHp; // hp�� �ش� phaseHp�� �ɶ����� ����� �Ѿ��.

    [Header("Status")]
    [SerializeField]
    private int maxHp;
    private int hp;
    
    [HideInInspector]
    public bool onDie = false;
    [SerializeField]
    private float patternDelay; // ���� ������ ������

    [Header("Move")]
    [SerializeField]
    private float attackDisMin = 7.2f; // �������� ������ ���� �ּҰ�
    [SerializeField]
    private float attackDisMax = 8.8f; // �������� ������ ���� �ִ밪
    [SerializeField]
    private float[] attackPosY; // ������ ������ ������Y
    [SerializeField]
    private float moveSpeed;

    [Header("Attack")]
    [SerializeField]
    private Transform emitter;
    [SerializeField]
    private float attackDelay; // ������ ������. � �����̵� ���� �ð��� �ߵ��Ѵ�.
    [SerializeField]
    private float shotDelay; // �⺻ ������ ������

    [Header("Attack_GeneralShot")]
    [SerializeField]
    private float generalShotSpeed;

    [Header("Attack_ImpactShot")]
    [SerializeField]
    private float impactShotSpeed;

    [Header("Attack_ComboShot")]
    [SerializeField]
    private int comboShotCount;
    [SerializeField]
    private float comboShotDelay = 0.3f; // Ʈ���ü� ���� ���� ������

    [Header("Attack_FlameSpear")]
    [SerializeField]
    private float flameSpearDis = 1f; // �÷��� ���Ǿ�� �÷��̾�� �ִ�� ���� �ϴ� �Ÿ�
    [SerializeField]
    private float flameSpearSpeed = 2f;

    [Header("Attack_MachStrike")]
    [SerializeField]
    private float machStrikeStartDisX = 22.4f; // ���� ��ø� �����ϱ� ���� �÷��̾���� X ����
    [SerializeField]
    private int machStrikeCount = 5;
    private int machStrikeDirIndex = 4; // ���� ������ ������ (���� �̵� 2, �밢�� �̵� 2)
    [SerializeField]
    private float machStrikeEndDisX = 8.6f; // ���� ��ø� ������ ���� �÷��̾���� X ����
    [SerializeField]
    private float machStrikeDirY = 0.2f; // �밢�� �̵����� Y �̵���.
    [SerializeField]
    private float machStrikeRotate; // �밢�� �̵����� rotation Z ��
    [SerializeField]
    private float machStrikeSpeed = 10f;
    [SerializeField]
    private float machStrikeStartDelay = 1f; // ���ϴ�� ���� ���� ������
    [SerializeField]
    private float machStrikeEndDelay = 0.3f; // ���ϴ�� ���� ���� ������
    [SerializeField]
    private Slider[] machStrikePaths;

    // yield return time
    private WaitForSeconds attackWait;
    private WaitForSeconds patternWait;
    private WaitForSeconds shotWait;
    private WaitForSeconds comboShotWait;
    private WaitForSeconds machDashStartWait;
    private WaitForSeconds machDashEndWait;

    [Header("Component")]
    private SpriteRenderer sprite;
    private Animator animator;
    private Rigidbody2D rigid;
    private BoxCollider2D collider;
    private B_ExcelTurbo turbo;
    private GameObject player;

    void Awake()
    {
        sprite = GetComponent<SpriteRenderer>();
        animator = GetComponent<Animator>();
        rigid = GetComponent<Rigidbody2D>();
        collider = GetComponent<BoxCollider2D>();
        turbo = GetComponentInChildren<B_ExcelTurbo>();
        player = GameObject.Find("Player");
    }

    void Start()
    {
        Init();

        StartCoroutine("PatternCycle");
    }

    void Init()
    {
        phase = Phase.Phase1;
        hp = maxHp;
        turbo.ControlEngine(true);
        UIManager.instance.BossHPModify(true, maxHp, maxHp); // BossHP UI�� �ʱ�ȭ�Ѵ�

        attackWait = new WaitForSeconds(attackDelay);
        patternWait = new WaitForSeconds(patternDelay);
        shotWait = new WaitForSeconds(shotDelay);
        comboShotWait = new WaitForSeconds(comboShotDelay);
        machDashStartWait = new WaitForSeconds(machStrikeStartDelay);
        machDashEndWait = new WaitForSeconds(machStrikeEndDelay);
    }

    IEnumerator PatternCycle()
    {
        if (onDie)
            yield break;

        yield return patternWait;

        if (rigid.position.x - player.transform.position.x > attackDisMax) // �÷��̾�� �ָ� ���� �̵�
        {
            StartCoroutine("Move", Vector2.left);
        }
        else if (rigid.position.x - player.transform.position.x < attackDisMin) // �÷��̾�� ������ ������ �̵�
        {
            StartCoroutine("Move", Vector2.right);
        }
        else // ���� ��ġ�� ��� ���� ���
        {
            int pattern = PatternChoice();

            switch (phase)
            {
                case Phase.Phase1: // 1������ ���� : GeneralShot(80%), ImpactShot(20%)
                    switch (pattern)
                    {
                        case 0:
                        case 1:
                        case 2:
                        case 3:
                            StartCoroutine("GeneralShot");
                            break;
                        case 4:
                            StartCoroutine("ImpactShot");
                            break;
                    }
                    break;

                case Phase.Phase2: // 2������ ���� : ImpactShot(60%), ComboShot(40%)
                    switch (pattern)
                    {
                        case 0:
                        case 1:
                        case 2:
                            StartCoroutine("ImpactShot");
                            break;
                        case 3:
                        case 4:
                            StartCoroutine("ComboShot");
                            break;
                    }
                    break;
                
                case Phase.Phase3: // 3������ ���� : ImpactShot(10%), FlameSpear(50%), MachStrike(40%)
                    switch (pattern)
                    {
                        case 0:
                            StartCoroutine("ImpactShot");
                            break;
                        case 1:
                        case 2:
                        case 3:
                        case 4:
                        case 5:
                            StartCoroutine("FlameSpear");
                            break;
                        case 6:
                        case 7:
                        case 8:
                        case 9:
                            StartCoroutine("MachStrike");
                            break;
                    }
                    break;
            }
        }
    }

    int PatternChoice()
    {
        int patternMin = 0;
        int patternMax = 0;

        switch (phase)
        {
            case Phase.Phase1:
            case Phase.Phase2:
                patternMin = 0;
                patternMax = 5;
                break;

            case Phase.Phase3:
                patternMin = 0;
                patternMax = 10;
                break;
        }
        
        int patternIndex = Random.Range(patternMin, patternMax);
        return patternIndex;
    }

    IEnumerator Move(Vector2 dir)
    {
        while (true)
        {
            rigid.velocity = dir * moveSpeed;
            float distance = rigid.position.x - player.transform.position.x;

            if (distance <= attackDisMax && distance >= attackDisMin)
            {
                break;
            }

            yield return null;
        }

        rigid.velocity = Vector2.zero;
        animator.SetBool("onDrive", false);
        StartCoroutine("PatternCycle");
    }

    IEnumerator MoveMaxDis() // �ִ� ��Ÿ��� �̵��Ѵ�.
    {
        float movePosX = player.transform.position.x + attackDisMax; // ����Ʈ���� �ִ� ��Ÿ��� �̵��� ���.

        while (rigid.position.x <= movePosX)
        {
            rigid.velocity = Vector2.right * moveSpeed;
            yield return null;
        }

        rigid.velocity = Vector2.zero;
    }

    IEnumerator GeneralShot()
    {
        int shotCount = Random.Range(1, 3); // �ִ� 2�߱��� ���.

        for (int i = 0; i < shotCount; i++)
        {
            yield return shotWait;
            GameObject spawnBullet = PoolManager.instance.Get(PoolManager.PoolType.Bullet, 2);
            spawnBullet.transform.position = emitter.position;
            spawnBullet.GetComponent<Bullet>().Shoot(Vector2.left, generalShotSpeed);
            AudioManager.instance.PlayEnemySFX(AudioManager.EnemySfx.ExcelGeneralShot);
        }

        yield return attackWait;
        StartCoroutine("PatternCycle");
    }

    IEnumerator ImpactShot()
    {
        yield return StartCoroutine("MoveMaxDis"); // �ִ� ��Ÿ��� �̵�
        yield return shotWait;
        GameObject spawnBullet = PoolManager.instance.Get(PoolManager.PoolType.Bullet, 3);
        spawnBullet.transform.position = emitter.position;
        spawnBullet.GetComponent<Bullet>().Shoot(Vector2.left, impactShotSpeed);
        AudioManager.instance.PlayEnemySFX(AudioManager.EnemySfx.ExcelImpactShot);

        yield return attackWait;
        StartCoroutine("PatternCycle");
    }

    IEnumerator ComboShot()
    {
        yield return StartCoroutine("MoveMaxDis"); // �ִ� ��Ÿ��� �̵�
        int randomNum = -1;

        for (int i = 0; i < comboShotCount; i++) // comboShotCount ��ŭ �ݺ��Ѵ�.
        {
            if (i == 0)
            {
                randomNum = 0; // ù ���� ���ڸ����� ���
            }
            else
            {
                randomNum = ComboShotPos(randomNum);
            }
            
            float posY = attackPosY[randomNum];
            
            if (rigid.position.y > posY) // �Ʒ��� �̵�
            {
                while (true)
                {
                    rigid.velocity = Vector2.down * moveSpeed;

                    if (rigid.position.y <= posY)
                    {
                        break;
                    }

                    yield return null;
                }
            }
            else if (rigid.position.y < posY) // ���� �̵�
            {
                while (true)
                {
                    rigid.velocity = Vector2.up * moveSpeed;

                    if (rigid.position.y >= posY)
                    {
                        break;
                    }

                    yield return null;
                }
            }

            rigid.velocity = Vector2.zero;
            GameObject spawnBullet = PoolManager.instance.Get(PoolManager.PoolType.Bullet, 2);
            spawnBullet.transform.position = emitter.position;
            spawnBullet.GetComponent<Bullet>().Shoot(Vector2.left, generalShotSpeed);
            AudioManager.instance.PlayEnemySFX(AudioManager.EnemySfx.ExcelGeneralShot);
            yield return comboShotWait;
        }

        if (rigid.position.y > attackPosY[0]) // ������ ���ڸ��� �̵�
        {
            while (true)
            {
                rigid.velocity = Vector2.down * moveSpeed;

                if (rigid.position.y <= attackPosY[0])
                {
                    break;
                }

                yield return null;
            }
        }

        rigid.velocity = Vector2.zero;
        yield return attackWait;
        StartCoroutine("PatternCycle");
    }

    int ComboShotPos(int num)
    {
        int randomNum = 0;

        while (true)
        {
            randomNum = Random.Range(0, attackPosY.Length);

            if (randomNum == num)
                continue;
            else
                break;
        }

        return randomNum;
    }

    IEnumerator FlameSpear()
    {
        yield return StartCoroutine("MoveMaxDis"); // �ִ� ��Ÿ��� �̵�
        animator.SetBool("onDrive", true);
        turbo.ControlEngine(false);
        turbo.BoostStart();

        while (true)
        {
            rigid.velocity = Vector2.left * flameSpearSpeed;
            float dis = rigid.position.x - player.transform.position.x;

            if (dis <= flameSpearDis || rigid.position.x < player.transform.position.x) // �ʹ� �����ų� �÷��̾�� �������� �� ��� ����
            {
                break;
            }

            yield return null;
        }

        turbo.BoostEnd();
        turbo.ControlEngine(true);
        rigid.velocity = Vector2.zero;
        yield return attackWait;
        yield return StartCoroutine("Move", Vector2.right); // Move�� ���������� PatternCycle�� ��ü�Ѵ�.
    }

    IEnumerator MachStrike()
    {
        animator.SetBool("onDetect", true);
        float targetPosX = player.transform.position.x + machStrikeStartDisX;

        while (rigid.position.x <= targetPosX) // ����� �����ϱ� ���� ī�޶� ������ �̵�
        {
            rigid.velocity = Vector2.right * moveSpeed;
            yield return null;
        }

        rigid.velocity = Vector2.zero;
        
        yield return attackWait; // ��� �����̸� �ش�.
        transform.localScale = new Vector3(transform.localScale.x * -1, transform.localScale.y, transform.localScale.z);
        animator.SetBool("onDetect", false);
        animator.SetBool("onDrive", true);
        Vector2 startPos = transform.position; // ������ ������ ���� ��ġ

        for (int i = 0; i < machStrikeCount; i++) // machStrikeCount ����ŭ �ݺ��Ѵ�.
        {
            int attackIndex = Random.Range(0, machStrikeDirIndex);
            float endXPos = player.transform.position.x - machStrikeEndDisX;
            Vector2 attackPos = Vector2.zero;
            Vector2 dashDir = Vector2.zero; // ���� ������ ����
            Quaternion attackRotation = Quaternion.identity;

            switch (attackIndex)
            {
                case 0: // ���� ���� �̵�
                    attackPos = new Vector2(startPos.x, attackPosY[0]);
                    dashDir = Vector2.left;
                    break;
                case 1: // ���� ���� �̵�
                    attackPos = new Vector2(startPos.x, attackPosY[1]);
                    dashDir = Vector2.left;
                    break;
                case 2: // ���� �밢�� �̵�
                    attackPos = new Vector2(startPos.x, attackPosY[0]);
                    dashDir = new Vector2(-1, machStrikeDirY);
                    attackRotation = Quaternion.Euler(new Vector3(0, 0, -machStrikeRotate));
                    break;
                case 3: // ���� �밢�� �̵�
                    attackPos = new Vector2(startPos.x, attackPosY[1]);
                    dashDir = new Vector2(-1, -machStrikeDirY);
                    attackRotation = Quaternion.Euler(new Vector3(0, 0, machStrikeRotate));
                    break;
            }

            machStrikePaths[attackIndex].gameObject.SetActive(true); // ���� ��� UI Ȱ��ȭ
            transform.position = attackPos;
            transform.rotation = attackRotation;
            
            yield return machDashStartWait; // UI�� ���� ������ �ð��� �ش�.
            machStrikePaths[attackIndex].gameObject.SetActive(false); // ���� ��� UI ��Ȱ��ȭ

            AudioManager.instance.PlayEnemySFX(AudioManager.EnemySfx.ExcelMachStrike);

            while (rigid.position.x >= endXPos)
            {
                rigid.velocity = dashDir * machStrikeSpeed;
                yield return null;
            }

            rigid.velocity = Vector2.zero;
            yield return machDashEndWait; // ���� ���� ���� ���� �д�
        }

        transform.position = startPos; // �ٽ� ��ġ�� ���� ũ�⸦ �ʱ�ȭ�Ѵ�.
        transform.rotation = Quaternion.identity;
        transform.localScale = new Vector3(transform.localScale.x * -1, transform.localScale.y, transform.localScale.z);

        yield return attackWait;
        StartCoroutine("PatternCycle");
    }

    public void Damage()
    {
        hp--;

        if (hp <= 0)
        {
            UIManager.instance.BossHPModify(false);
            Die();
        }
        else
        {
            UIManager.instance.BossHPModify(true, maxHp, hp);

            if (hp >= phaseHp[0])
            {
                phase = Phase.Phase1;
            }
            else if (hp >= phaseHp[1])
            {
                phase = Phase.Phase2;
            }
            else
            {
                phase = Phase.Phase3;
            }
        }
    }

    void Die()
    {
        StopAllCoroutines();
        onDie = true;
        BossStageManager.instance.BossDefeat();
    }
}