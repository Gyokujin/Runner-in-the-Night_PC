using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    // Status
    private float landDis;
    private float jumpTime;
    private float jumpCool;
    private float jumpFoece;
    private float slideTime;
    private float slideCool;
    private float attackCool;
    private float hitTime;
    private float putGunCool;
    private int maxLife;
    private float knockback;
    private float bounce;
    private float invincibleTime;
    private float respawnTime;

    // Action
    private int jumpCount = 2;
    private bool onJumping = false;
    private bool jumpAble = true;
    private Transform[] landVec = new Transform[2];
    private bool onGround = false;
    private bool onFall = false;
    private bool onSlide = false;
    [SerializeField]
    private int defaultLayer; // �÷��̾� ���̾�
    private bool onInvincible;
    [SerializeField]
    private int invincibleLayer; // �������� ���̾�
    private float leftInvincible = 0; // ���� ���� ���� �ð��� ���
    [SerializeField]
    private float respawnPosY = 7f; // ����� ���� �������ÿ� �̵���ų Y ��ǥ

    // Attack
    private bool onAttack = false;
    [SerializeField]
    private Transform emitter;
    [SerializeField]
    private GameObject bullet;
    [SerializeField]
    private float bulletSpeed;

    // Hit
    [SerializeField]
    private int life;
    [HideInInspector]
    public bool onDamage = false;
    [HideInInspector]
    public bool isDead = false;

    // yield return time
    private WaitForSeconds slideWait;
    private WaitForSeconds attackWait;
    private WaitForSeconds hitWait;
    private WaitForSeconds putGunWait;
    private WaitForSeconds respawnWait;

    // Component
    private PlayerStatus status;
    private SpriteRenderer sprite;
    private Rigidbody2D rigid;
    private BoxCollider2D collider;
    private Animator animator;

    void Awake()
    {
        status = GetComponent<PlayerStatus>();
        sprite = GetComponent<SpriteRenderer>();
        rigid = GetComponent<Rigidbody2D>();
        collider = GetComponent<BoxCollider2D>();
        animator = GetComponent<Animator>();
    }

    void Start()
    {
        StatusSetting();
        StartSetting();
        WaitTimeSetting();
    }

    void StatusSetting()
    {
        landDis = status.RayDistance;
        jumpTime = status.JumpTime;
        jumpCool = status.JumpCoolTime;
        jumpFoece = status.JumpFoce;
        slideTime = status.SlideTime;
        slideCool = status.SlideCoolTime;
        attackCool = status.AttackCoolTime;
        hitTime = status.HitTime;
        putGunCool = status.PutGunTime;
        maxLife = status.MaxLife;
        knockback = status.KnockbackForce;
        bounce = status.BounceForce;
        invincibleTime = status.InvincibleTime;
        respawnTime = status.RespawnTime;
    }

    void StartSetting()
    {
        life = maxLife;
        landVec[0] = transform.GetChild(0);
        landVec[1] = transform.GetChild(1);
        gameObject.layer = defaultLayer;
    }

    void WaitTimeSetting()
    {
        slideWait = new WaitForSeconds(slideTime);
        attackWait = new WaitForSeconds(attackCool);
        hitWait = new WaitForSeconds(hitTime);
        putGunWait = new WaitForSeconds(putGunCool);
        respawnWait = new WaitForSeconds(respawnTime);
    }

    void Update()
    {
        if (isDead)
            return;

        if (!onDamage && !onGround && !onJumping && !onSlide && GroundCheck(landDis))
        {
            Land();
        }

        Fall(rigid.velocity.y < -0.1f ? true : false); // 0�� ���� ���� �ݵ����� �ִϸ��̼� ������ ��Ÿ����.
        Invincible();
    }

    public void Move(bool move)
    {
        animator.SetBool("onMove", move);
    }

    void Land()
    {
        onGround = true;
        AttackCancel();

        if (jumpCount != 2)
        {
            jumpCount = 2;
            UIManager.instance.JumpCount(jumpCount);
        }

        animator.SetBool("onGround", true);
        animator.SetBool("onFall", false);
    }

    bool GroundCheck(float distance)
    {
        Vector2 landPos1 = new Vector2(transform.position.x + landVec[0].localPosition.x, transform.position.y + landVec[0].localPosition.y);
        Vector2 landPos2 = new Vector2(transform.position.x + landVec[1].localPosition.x, transform.position.y + landVec[1].localPosition.y);
        Debug.DrawRay(landPos1, Vector2.down * distance, Color.green);
        Debug.DrawRay(landPos2, Vector2.down * distance, Color.green);
        RaycastHit2D platCheck1 = Physics2D.Raycast(landPos1, Vector2.down, distance, LayerMask.GetMask("Ground"));
        RaycastHit2D platCheck2 = Physics2D.Raycast(landPos2, Vector2.down, distance, LayerMask.GetMask("Ground"));

        return platCheck1 || platCheck2 ? true : false;
    }

    public void Jump() // ��ư���� ����ϱ� ������ public
    {
        if (!isDead && jumpCount > 0 && jumpAble && !onDamage) // 2�� �������� �����ϴ�.
        {
            if (onSlide)
            {
                SlideCancel();
            }

            AttackCancel();
            onGround = false;
            onJumping = true;
            onFall = false;
            jumpAble = false;
            jumpCount--;
            UIManager.instance.JumpCount(jumpCount);
            animator.SetBool("onGround", false);

            rigid.velocity = Vector2.zero;
            rigid.AddForce(Vector2.up * jumpFoece);
            animator.SetTrigger("doJump");
            AudioManager.instance.PlayPlayerSFX(AudioManager.PlayerSFX.Jump);
            Invoke("JumpCool", jumpCool);
            Invoke("JumpTime", jumpTime); // �������� Jump�� ���� ������ �������� onJump�� �����̸� �ش�.
        }
    }

    public void OnJump(bool click) // ��ư���� ����ϱ� ������ public
    {
        rigid.gravityScale = click ? 0.75f : 1.1f;
    }

    void JumpCool()
    {
        jumpAble = true;
    }

    void JumpTime()
    {
        onJumping = false;
    }

    void Fall(bool fall)
    {
        onFall = fall;
        animator.SetBool("onFall", fall);
    }

    public void Slide()
    {
        if (!isDead && GroundCheck(landDis) && !onDamage && !onSlide)
        {
            StartCoroutine("SlideProcess");
        }

        if (onAttack)
        {
            AttackCancel();
        }
    }

    IEnumerator SlideProcess()
    {
        onSlide = true;
        animator.SetBool("onSlide", true);
        leftInvincible += slideTime;
        AudioManager.instance.PlayPlayerSFX(AudioManager.PlayerSFX.Slide);
        UIManager.instance.ButtonCooldown("slide", slideCool); // �����̵��� ��Ÿ�� ���

        yield return slideWait;
        onSlide = false;
        animator.SetBool("onSlide", false);
    }

    void SlideCancel()
    {
        StopCoroutine("SlideProcess"); // �����̵��� ������ ������ bool�� �ʱ�ȭ�Ѵ�.
        onSlide = false;
        animator.SetBool("onSlide", false);
        gameObject.layer = defaultLayer;
    }

    public void Attack()
    {
        if (!onAttack && !isDead && !onDamage && !onSlide)
        {
            StopCoroutine("AttackProcess");
            StartCoroutine("AttackProcess");
        }
    }

    IEnumerator AttackProcess()
    {
        onAttack = true;
        animator.SetBool("onAttack", true);
        GameObject spawnBullet = PoolManager.instance.Get(PoolManager.PoolType.Bullet, 0);
        spawnBullet.transform.position = emitter.position;
        spawnBullet.GetComponent<Bullet>().Shoot(Vector2.right, bulletSpeed);
        AudioManager.instance.PlayPlayerSFX(AudioManager.PlayerSFX.Shoot);

        yield return attackWait;
        onAttack = false;

        yield return putGunWait;
        animator.SetBool("onAttack", false);
    }

    void AttackCancel()
    {
        StopCoroutine("AttackProcess");
        onAttack = false;
        animator.SetBool("onAttack", false);
    }

    public void Hit()
    {
        if (!onInvincible && !onDamage)
        {
            StartCoroutine("HitProcess");
        }
    }

    IEnumerator HitProcess()
    {
        GameManager.instance.GameLive(false); // ��ũ�Ѹ� �Ͻ�����
        onDamage = true;
        Move(false);
        rigid.velocity = Vector2.zero;
        AttackCancel();
        LoseHP();

        if (life <= 0)
        {
            Die();
        }
        else
        {
            animator.SetTrigger("onHit");
            rigid.AddForce(Vector2.left * knockback);
            AudioManager.instance.PlayPlayerSFX(AudioManager.PlayerSFX.Hit);

            yield return hitWait;

            if (!GameManager.instance.gameFinish)
            {
                Move(true);
                GameManager.instance.GameLive(true);
            }

            sprite.color = new Color(1, 1, 1, 0.7f);
            onDamage = false;

            leftInvincible += invincibleTime;
            yield return new WaitForSeconds(leftInvincible);
            sprite.color = new Color(1, 1, 1, 1);
        }
    }

    public void FallDown()
    {
        if (!onInvincible && !onDamage)
        {
            LoseHP();
        }

        onDamage = true;

        if (life <= 0)
        {
            Die();
        }
        else
        {
            StartCoroutine("Respawn");
        }
    }

    void LoseHP()
    {
        life--;
        UIManager.instance.DamageUI(life);
    }

    IEnumerator Respawn()
    {
        yield return respawnWait;

        if (!isDead)
        {
            rigid.simulated = false; // ������ ó�� �߿��� rigidbody�� ��Ȱ��ȭ�Ѵ�.
            sprite.enabled = false;
            transform.position = new Vector2(transform.position.x, respawnPosY);
            GameManager.instance.GameLive(true);

            while (!GroundCheck(10f))
            {
                yield return null;
            }

            onDamage = false;
            rigid.simulated = true;
            rigid.velocity = Vector2.zero;
            sprite.enabled = true;
            sprite.color = new Color(1, 1, 1, 0.7f);
            UIManager.instance.RespawnFX(transform.position);
            AudioManager.instance.PlayPlayerSFX(AudioManager.PlayerSFX.Respawn);

            leftInvincible += invincibleTime;
            yield return new WaitForSeconds(leftInvincible);
            sprite.color = new Color(1, 1, 1, 1);
        }
    }

    void Die()
    {
        if (isDead)
            return;

        GameManager.instance.isLive = false;
        isDead = true;
        collider.enabled = false;
        animator.SetTrigger("doDie");
        AudioManager.instance.PlayPlayerSFX(AudioManager.PlayerSFX.Die);
        StartCoroutine("DieProcess");
    }

    IEnumerator DieProcess()
    {
        rigid.velocity = Vector2.zero;
        rigid.AddForce(Vector2.up * bounce);

        yield return new WaitForSeconds(1f);
        rigid.gravityScale *= 3;
        GameManager.instance.GameOver();

        yield return new WaitForSeconds(2f); // ĳ���Ͱ� ȭ�� ������ ���� �������� �߶��� ��Ȱ��ȭ �Ѵ�.
        rigid.simulated = false;
    }

    void Invincible()
    {
        if (GameManager.instance.isLive)
        {
            if (leftInvincible > 0)
            {
                onInvincible = true;
                gameObject.layer = invincibleLayer;
                leftInvincible -= Time.deltaTime;
            }
            else
            {
                onInvincible = false;
                gameObject.layer = defaultLayer;
            }
        }
    }
}