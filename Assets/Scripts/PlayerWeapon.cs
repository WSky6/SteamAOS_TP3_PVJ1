using UnityEngine;
using UnityEngine.InputSystem;

// ============================================================================
//  ARMA DEL PLAYER   <-- ACÁ SE MANEJA LA MUNICIÓN (para quien haga el HUD)
//
//  Leer desde el HUD:
//    PlayerWeapon.Ammo            balas que quedan en el cargador
//    PlayerWeapon.MagazineSize    tamaño del cargador
//    PlayerWeapon.IsReloading     true mientras recarga
//    PlayerWeapon.ReloadProgress  de 0 a 1 (para una barrita de recarga)
//  O suscribirse a los eventos:
//    OnAmmoChanged(actual, cargador)   cada vez que cambia la munición
//    OnReloadStarted() / OnReloadFinished()
//
//  Nota: el arma se agrega sola al player unos instantes después de empezar
//  (script PlayerSetup), así que conviene LEER las propiedades en Update
//  y no solo suscribirse en Start.
//
//  Controles: clic izquierdo dispara, R recarga (también recarga sola al vaciarse).
//  Este script NO dibuja nada en pantalla, salvo el puntero (Show Crosshair).
// ============================================================================
public class PlayerWeapon : MonoBehaviour
{
    [Header("Disparo")]
    [SerializeField] private float damage = 25f;
    [SerializeField] private float bulletSpeed = 40f;
    [SerializeField] private float fireCooldown = 0.35f;
    [SerializeField] private Color bulletColor = new Color(0.2f, 0.9f, 1f);

    [Header("Munición")]
    [SerializeField] private int magazineSize = 5;
    [SerializeField] private float reloadTime = 1.2f;

    [Header("Animación de recarga")]
    [SerializeField] private float reloadDip = 0.3f;     // cuánto baja el arma
    [SerializeField] private float reloadTilt = 55f;     // cuánto se inclina hacia abajo (grados)

    [Header("Arma (palito)")]
    [SerializeField] private Vector3 stickPosition = new Vector3(0.3f, -0.28f, 0.55f);
    [SerializeField] private float stickLength = 0.7f;
    [SerializeField] private float stickThickness = 0.08f;
    [SerializeField] private Color stickColor = new Color(0.12f, 0.12f, 0.12f);
    [SerializeField] private Color tipColor = new Color(0.2f, 0.9f, 1f);

    [Header("Puntero")]
    [SerializeField] private bool showCrosshair = true;
    [SerializeField] private float crosshairSize = 7f;
    [SerializeField] private float crosshairGap = 4f;
    [SerializeField] private float crosshairThickness = 2f;

    static readonly RaycastHit[] hits = new RaycastHit[16];

    private Camera cam;
    private Transform pivot;
    private Transform muzzle;
    private float nextFire;
    private float recoilUntil;
    private bool warnedNoCamera;

    // ---------------- MUNICIÓN (estado que lee el HUD) ----------------
    private int ammo;
    private bool reloading;
    private float reloadStart;

    public int Ammo => ammo;
    public int MagazineSize => magazineSize;
    public bool IsReloading => reloading;
    public float ReloadProgress => reloading
        ? Mathf.Clamp01((Time.time - reloadStart) / Mathf.Max(0.1f, reloadTime))
        : 1f;

    public event System.Action<int, int> OnAmmoChanged;  // (balas actuales, tamaño del cargador)
    public event System.Action OnReloadStarted;
    public event System.Action OnReloadFinished;

    void Awake()
    {
        ammo = magazineSize;
    }

    void Start()
    {
        OnAmmoChanged?.Invoke(ammo, magazineSize);
    }

    void SetAmmo(int value)
    {
        ammo = Mathf.Clamp(value, 0, magazineSize);
        OnAmmoChanged?.Invoke(ammo, magazineSize);
    }

    void StartReload()
    {
        if (reloading) return;

        reloading = true;
        reloadStart = Time.time;
        OnReloadStarted?.Invoke();
    }

    void FinishReload()
    {
        reloading = false;
        SetAmmo(magazineSize);
        OnReloadFinished?.Invoke();
    }

    void Update()
    {
        // Si no hay cámara o se destruyó (cambio de escena), la busca y arma el palito de nuevo
        if (cam == null || pivot == null)
        {
            cam = FindCamera();

            if (cam == null)
            {
                if (!warnedNoCamera)
                {
                    Debug.LogWarning("PlayerWeapon: no encuentro ninguna cámara activa.");
                    warnedNoCamera = true;
                }
                return;
            }

            BuildStick();
        }

        if (Time.timeScale == 0f) return; // en pausa no dispara

        // ---------------- RECARGA ----------------
        float dip = 0f;

        if (reloading)
        {
            float p = ReloadProgress;

            if (p >= 1f) FinishReload();
            else dip = Mathf.Sin(p * Mathf.PI); // baja y vuelve a subir
        }
        else
        {
            bool reloadKey = Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame;
            if (reloadKey && ammo < magazineSize) StartReload();

            // Cargador vacío: recarga sola
            if (ammo <= 0 && Time.time >= nextFire) StartReload();
        }

        // ---------------- ANIMACIÓN DEL ARMA ----------------
        float recoil = Time.time < recoilUntil ? -0.06f : 0f;
        Vector3 targetPos = stickPosition + new Vector3(0f, -reloadDip * dip, recoil);
        Quaternion targetRot = Quaternion.Euler(reloadTilt * dip, 0f, 0f);

        pivot.localPosition = Vector3.Lerp(pivot.localPosition, targetPos, 25f * Time.deltaTime);
        pivot.localRotation = Quaternion.Slerp(pivot.localRotation, targetRot, 25f * Time.deltaTime);

        // ---------------- DISPARO ----------------
        bool click = Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;
        if (click && !reloading && ammo > 0 && Time.time >= nextFire) Fire();
    }

    Camera FindCamera()
    {
        if (Camera.main != null) return Camera.main;

        foreach (Camera c in Camera.allCameras)
        {
            if (c.enabled && c.gameObject.activeInHierarchy) return c;
        }
        return null;
    }

    void BuildStick()
    {
        pivot = new GameObject("ArmaPlayer").transform;
        pivot.SetParent(cam.transform, false);
        pivot.localPosition = stickPosition;

        // Cuerpo del arma
        GameObject stick = GameObject.CreatePrimitive(PrimitiveType.Cube);
        stick.name = "Palito";
        Destroy(stick.GetComponent<Collider>());
        stick.transform.SetParent(pivot, false);
        stick.transform.localPosition = new Vector3(0f, 0f, stickLength * 0.5f);
        stick.transform.localScale = new Vector3(stickThickness, stickThickness, stickLength);
        stick.GetComponent<Renderer>().material.color = stickColor;

        // Punta brillante: de ahí sale la bala
        GameObject tip = GameObject.CreatePrimitive(PrimitiveType.Cube);
        tip.name = "Punta";
        Destroy(tip.GetComponent<Collider>());
        tip.transform.SetParent(pivot, false);
        tip.transform.localPosition = new Vector3(0f, 0f, stickLength);
        tip.transform.localScale = Vector3.one * (stickThickness * 1.2f);
        tip.GetComponent<Renderer>().material.color = tipColor;

        muzzle = new GameObject("Muzzle").transform;
        muzzle.SetParent(pivot, false);
        muzzle.localPosition = new Vector3(0f, 0f, stickLength + stickThickness * 0.6f);
    }

    void Fire()
    {
        nextFire = Time.time + fireCooldown;
        recoilUntil = Time.time + 0.08f;
        SetAmmo(ammo - 1);

        Vector3 origin = cam.transform.position;
        Vector3 camDir = cam.transform.forward;

        // Punto al que apunta el puntero (centro de la pantalla)
        Vector3 target = origin + camDir * 100f;
        int n = Physics.RaycastNonAlloc(origin, camDir, hits, 200f, ~0, QueryTriggerInteraction.Ignore);
        float best = float.MaxValue;

        for (int i = 0; i < n; i++)
        {
            Transform t = hits[i].transform;
            if (t.IsChildOf(transform) || t.IsChildOf(cam.transform)) continue;
            if (hits[i].distance < best) best = hits[i].distance;
        }
        if (best < float.MaxValue) target = origin + camDir * best;

        // La bala sale de la punta del palito y viaja hacia el punto del puntero
        Vector3 muzzlePos = muzzle.position;
        Vector3 dir = (target - muzzlePos).normalized;
        if (Vector3.Dot(dir, camDir) < 0.5f) dir = camDir; // objetivo pegado: tira recto

        PlayerBullet.Spawn(muzzlePos, dir, bulletSpeed, damage, bulletColor, transform);
    }

    // ---------------- PUNTERO (lo único que dibuja este script) ----------------

    void OnGUI()
    {
        if (!showCrosshair || Time.timeScale == 0f) return;

        float cx = Screen.width * 0.5f;
        float cy = Screen.height * 0.5f;
        Texture2D white = Texture2D.whiteTexture;

        // Contorno negro
        GUI.color = new Color(0f, 0f, 0f, 0.8f);
        DrawCross(cx, cy, white, 1.5f);

        // Cruz blanca
        GUI.color = Color.white;
        DrawCross(cx, cy, white, 0f);

        GUI.color = Color.white;
    }

    void DrawCross(float cx, float cy, Texture2D tex, float extra)
    {
        float t = crosshairThickness + extra * 2f;
        float s = crosshairSize + extra;
        float g = crosshairGap - extra;

        GUI.DrawTexture(new Rect(cx - t * 0.5f, cy - g - s, t, s), tex);        // arriba
        GUI.DrawTexture(new Rect(cx - t * 0.5f, cy + g, t, s), tex);            // abajo
        GUI.DrawTexture(new Rect(cx - g - s, cy - t * 0.5f, s, t), tex);        // izquierda
        GUI.DrawTexture(new Rect(cx + g, cy - t * 0.5f, s, t), tex);            // derecha
    }
}