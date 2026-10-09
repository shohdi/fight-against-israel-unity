using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;

namespace FightAgainstIsrael
{
    public sealed class FighterGame : MonoBehaviour
    {
        public enum FlightState { Menu, Playing, Paused, GameOver }
        public FlightState State { get; private set; }
        public int Score { get; private set; }
        public int BestScore { get; private set; }
        public float FlightTime { get; private set; }
        public int EnemyCount => enemies.Count;
        public int ProjectileCount => bullets.Count;
        public bool MusicEnabled { get; private set; } = true;
        public bool EffectsEnabled { get; private set; } = true;
        public Camera FlightCamera => flightCamera;
        public Vector3 PlayerPosition => player == null ? Vector3.zero : player.transform.position;
        public const float FlightHeight = 4.5f;

        private Camera flightCamera;
        private Transform aircraftRoot;
        private GameObject player;
        private GameObject showcaseEnemy;
        private CombatHud hud;
        private GameAudio audioSystem;
        private ScrollingWorld world;
        private Material shotMaterial, enemyShotMaterial, flameMaterial, smokeMaterial;
        private readonly List<EnemyFlight> enemies = new List<EnemyFlight>(24);
        private readonly Stack<EnemyFlight> idleEnemies = new Stack<EnemyFlight>(24);
        private readonly List<Projectile> bullets = new List<Projectile>(128);
        private readonly Stack<Projectile> idleBullets = new Stack<Projectile>(128);
        private readonly List<Spark> sparks = new List<Spark>(180);
        private readonly Stack<Spark> idleSparks = new Stack<Spark>(180);
        private float nextSpawn, playerCooldown, shake;
        private int dragFinger = -1;
        private Vector3 dragOffset;
        private bool mouseDragging, touchFiring;
        private Vector3 cameraOrigin = new Vector3(0, 36, 0);
        private readonly Plane movementPlane = new Plane(Vector3.up, new Vector3(0, FlightHeight, 0));

        private sealed class EnemyFlight
        {
            public GameObject Visual;
            public float FireTimer, Speed, Phase, Drift;
        }
        private sealed class Projectile
        {
            public GameObject Visual;
            public bool Hostile;
            public float Speed;
        }
        private sealed class Spark
        {
            public GameObject Visual;
            public Vector3 Velocity;
            public float Life, Duration, Size;
            public bool Smoke;
        }

        private void Awake()
        {
            Application.targetFrameRate = 60;
            BestScore = PlayerPrefs.GetInt("FightAgainstIsrael.BestScore", 0);
            MusicEnabled = PlayerPrefs.GetInt("FightAgainstIsrael.Music", 1) == 1;
            EffectsEnabled = PlayerPrefs.GetInt("FightAgainstIsrael.Effects", 1) == 1;
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.52f, 0.64f, 0.69f);
            RenderSettings.fog = false;
            var sun = new GameObject("Golden hour sun").AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.color = new Color(1f, 0.89f, 0.71f);
            sun.intensity = 1.45f;
            sun.shadows = LightShadows.Soft;
            sun.shadowStrength = 0.65f;
            sun.transform.rotation = Quaternion.Euler(48, -35, 0);
            var cameraObject = new GameObject("Top-down flight camera");
            flightCamera = cameraObject.AddComponent<Camera>();
            cameraObject.AddComponent<AudioListener>();
            flightCamera.tag = "MainCamera";
            flightCamera.orthographic = true;
            flightCamera.orthographicSize = 14;
            flightCamera.nearClipPlane = 0.1f;
            flightCamera.farClipPlane = 100;
            flightCamera.backgroundColor = new Color(0.035f, 0.22f, 0.28f);
            flightCamera.clearFlags = CameraClearFlags.SolidColor;
            flightCamera.allowHDR = false;
            flightCamera.transform.SetPositionAndRotation(cameraOrigin, Quaternion.Euler(90, 0, 0));
            aircraftRoot = new GameObject("Aircraft").transform;
            player = AircraftFactory.CreatePlayer(aircraftRoot);
            showcaseEnemy = AircraftFactory.CreateEnemy(aircraftRoot);
            showcaseEnemy.transform.rotation = Quaternion.Euler(0, 180, 0);
            world = new GameObject("Coastal world").AddComponent<ScrollingWorld>();
            world.Initialize(flightCamera);
            audioSystem = gameObject.AddComponent<GameAudio>();
            audioSystem.Initialize();
            audioSystem.SetMusicEnabled(MusicEnabled);
            audioSystem.SetEffectsEnabled(EffectsEnabled);
            shotMaterial = GlowMaterial("Amber cannon tracer", new Color(1, 0.67f, 0.15f), 2);
            enemyShotMaterial = GlowMaterial("Red enemy tracer", new Color(1, 0.16f, 0.12f), 2);
            flameMaterial = GlowMaterial("Explosion embers", new Color(1, 0.36f, 0.05f), 1.7f);
            smokeMaterial = new Material(Shader.Find("Standard"));
            smokeMaterial.color = new Color(0.16f, 0.19f, 0.22f);
            hud = new GameObject("Flight interface").AddComponent<CombatHud>();
            hud.Initialize(this);
            ShowMenu();
        }

        private static Material GlowMaterial(string label, Color color, float emission)
        {
            var material = new Material(Shader.Find("Standard")) { name = label, color = color };
            material.EnableKeyword("_EMISSION");
            material.SetColor("_EmissionColor", color * emission);
            material.SetFloat("_Glossiness", 0.45f);
            return material;
        }

        public void StartFlight()
        {
            ClearCombat();
            Score = 0;
            FlightTime = 0;
            nextSpawn = 1.1f;
            playerCooldown = 0;
            shake = 0;
            dragFinger = -1;
            mouseDragging = touchFiring = false;
            player.SetActive(true);
            player.transform.SetPositionAndRotation(new Vector3(0, FlightHeight, -9), Quaternion.identity);
            showcaseEnemy.SetActive(false);
            State = FlightState.Playing;
            audioSystem.SetPlaying(true);
            hud.ShowPlaying();
        }

        public void ShowMenu()
        {
            ClearCombat();
            State = FlightState.Menu;
            player.SetActive(true);
            showcaseEnemy.SetActive(true);
            ArrangeShowcase();
            audioSystem.SetPlaying(false);
            hud.ShowMenu();
        }

        public void ArrangeShowcase()
        {
            bool portrait = Screen.width < Screen.height;
            player.transform.SetPositionAndRotation(new Vector3(portrait ? 2.5f : 5.4f, FlightHeight, portrait ? -7 : -5), Quaternion.Euler(0, -12, -5));
            showcaseEnemy.transform.position = new Vector3(portrait ? -2.5f : 8, FlightHeight, portrait ? -7 : 6);
        }

        public void TogglePause()
        {
            if (State == FlightState.Playing)
            {
                State = FlightState.Paused;
                audioSystem.SetPlaying(false);
                touchFiring = mouseDragging = false;
                dragFinger = -1;
                hud.ShowPause();
            }
            else if (State == FlightState.Paused)
            {
                State = FlightState.Playing;
                audioSystem.SetPlaying(true);
                hud.ShowPlaying();
            }
        }

        public void ToggleMusic()
        {
            MusicEnabled = !MusicEnabled;
            PlayerPrefs.SetInt("FightAgainstIsrael.Music", MusicEnabled ? 1 : 0);
            audioSystem.SetMusicEnabled(MusicEnabled);
            hud.RefreshAudioButtons();
        }
        public void ToggleEffects()
        {
            EffectsEnabled = !EffectsEnabled;
            PlayerPrefs.SetInt("FightAgainstIsrael.Effects", EffectsEnabled ? 1 : 0);
            audioSystem.SetEffectsEnabled(EffectsEnabled);
            hud.RefreshAudioButtons();
        }
        public void SetTouchFiring(bool firing) { touchFiring = firing; }

        private void Update()
        {
            float dt = Mathf.Min(Time.deltaTime, 0.05f);
            if (Input.GetKeyDown(KeyCode.M)) ToggleMusic();
            if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.P)) TogglePause();
            if ((State == FlightState.Menu || State == FlightState.GameOver) && Input.GetKeyDown(KeyCode.Return)) StartFlight();
            if (State != FlightState.Paused)
            {
                world.Tick(dt, State == FlightState.Playing ? 3.2f : 1.05f);
                TickSparks(dt);
            }
            if (State == FlightState.Menu)
            {
                var position = player.transform.position;
                position.y = FlightHeight + Mathf.Sin(Time.time * 1.4f) * 0.15f;
                player.transform.position = position;
            }
            if (State == FlightState.Playing)
            {
                FlightTime += dt;
                playerCooldown -= dt;
                MovePlayer(dt);
                if (Input.GetKey(KeyCode.Space) || touchFiring) Shoot();
                nextSpawn -= dt;
                if (nextSpawn <= 0 && enemies.Count < 20)
                {
                    SpawnEnemy();
                    nextSpawn = Mathf.Max(0.55f, 1.9f - FlightTime * 0.008f);
                }
                TickEnemies(dt);
                if (State == FlightState.Playing) TickBullets(dt);
            }
            shake = Mathf.MoveTowards(shake, 0, dt * 1.8f);
            var shakeOffset = State == FlightState.Paused ? Vector3.zero : new Vector3(Mathf.Sin(Time.time * 83), 0, Mathf.Cos(Time.time * 97)) * shake;
            flightCamera.transform.position = cameraOrigin + shakeOffset;
            hud.Tick(Score, BestScore, FlightTime);
            RunAutomatedChecks(dt);
        }

        private float HorizontalLimit => Mathf.Max(3, flightCamera.orthographicSize * flightCamera.aspect - 1.6f);
        private Vector3 ScreenToFlight(Vector2 position)
        {
            var ray = flightCamera.ScreenPointToRay(position);
            return movementPlane.Raycast(ray, out float distance) ? ray.GetPoint(distance) : player.transform.position;
        }
        private bool OverInterface(int fingerId = -1)
        {
            if (EventSystem.current == null) return false;
            return fingerId == -1 ? EventSystem.current.IsPointerOverGameObject() : EventSystem.current.IsPointerOverGameObject(fingerId);
        }

        private void MovePlayer(float dt)
        {
            var previous = player.transform.position;
            var position = previous;
            float horizontal = (Input.GetKey(KeyCode.RightArrow) || Input.GetKey(KeyCode.D) ? 1 : 0) -
                               (Input.GetKey(KeyCode.LeftArrow) || Input.GetKey(KeyCode.A) ? 1 : 0);
            float vertical = (Input.GetKey(KeyCode.UpArrow) || Input.GetKey(KeyCode.W) ? 1 : 0) -
                             (Input.GetKey(KeyCode.DownArrow) || Input.GetKey(KeyCode.S) ? 1 : 0);
            var direction = Vector2.ClampMagnitude(new Vector2(horizontal, vertical), 1);
            position += new Vector3(direction.x, 0, direction.y) * 10.5f * dt;
            for (int i = 0; i < Input.touchCount; i++)
            {
                var touch = Input.GetTouch(i);
                var touchPosition = ScreenToFlight(touch.position);
                if (touch.phase == TouchPhase.Began && !OverInterface(touch.fingerId))
                {
                    if (dragFinger == -1 && Vector3.Distance(touchPosition, previous) < 2.2f)
                    {
                        dragFinger = touch.fingerId;
                        dragOffset = previous - touchPosition;
                    }
                    else Shoot();
                }
                if (touch.fingerId == dragFinger)
                {
                    position = touchPosition + dragOffset;
                    if (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled) dragFinger = -1;
                }
            }
            if (Input.touchCount == 0)
            {
                if (Input.GetMouseButtonDown(0) && !OverInterface())
                {
                    var mousePosition = ScreenToFlight(Input.mousePosition);
                    mouseDragging = Vector3.Distance(mousePosition, previous) < 2.2f;
                    dragOffset = previous - mousePosition;
                    if (!mouseDragging) Shoot();
                }
                if (mouseDragging && Input.GetMouseButton(0)) position = ScreenToFlight(Input.mousePosition) + dragOffset;
                if (Input.GetMouseButtonUp(0)) mouseDragging = false;
            }
            position.x = Mathf.Clamp(position.x, -HorizontalLimit, HorizontalLimit);
            position.z = Mathf.Clamp(position.z, -11.4f, 11.4f);
            position.y = FlightHeight;
            player.transform.position = position;
            float bank = Mathf.Clamp((position.x - previous.x) / Mathf.Max(dt, 0.001f), -12, 12) * -1.6f;
            player.transform.rotation = Quaternion.Slerp(player.transform.rotation, Quaternion.Euler(0, 0, bank), dt * 9);
        }

        private void Shoot()
        {
            if (State != FlightState.Playing || playerCooldown > 0) return;
            playerCooldown = 0.14f;
            var origin = player.transform.position + new Vector3(-0.13f, 0.1f, 1.8f);
            SpawnBullet(origin, false);
            AddSpark(origin, new Vector3(0, 0, 4), 0.045f, 0.14f, false);
            audioSystem.PlayPlayerShot();
        }

        private void SpawnEnemy()
        {
            EnemyFlight enemy;
            if (idleEnemies.Count > 0) enemy = idleEnemies.Pop();
            else enemy = new EnemyFlight { Visual = AircraftFactory.CreateEnemy(aircraftRoot) };
            enemy.Visual.SetActive(true);
            enemy.Visual.transform.SetPositionAndRotation(new Vector3(UnityEngine.Random.Range(-HorizontalLimit, HorizontalLimit), FlightHeight, 16.5f), Quaternion.Euler(0, 180, 0));
            enemy.Speed = Mathf.Min(5.8f, 2.8f + FlightTime * 0.018f);
            enemy.FireTimer = UnityEngine.Random.Range(0.8f, 1.7f);
            enemy.Phase = UnityEngine.Random.Range(0, Mathf.PI * 2);
            enemy.Drift = UnityEngine.Random.Range(-0.45f, 0.45f);
            enemies.Add(enemy);
        }

        private void TickEnemies(float dt)
        {
            for (int i = enemies.Count - 1; i >= 0; i--)
            {
                var enemy = enemies[i];
                var position = enemy.Visual.transform.position;
                position.z -= enemy.Speed * dt;
                float drift = Mathf.Sin(FlightTime * 1.1f + enemy.Phase) * enemy.Drift;
                position.x = Mathf.Clamp(position.x + drift * dt, -HorizontalLimit, HorizontalLimit);
                enemy.Visual.transform.position = position;
                enemy.Visual.transform.rotation = Quaternion.Euler(0, 180, drift * 12);
                if (position.z < -17) { RecycleEnemy(i); continue; }
                enemy.FireTimer -= dt;
                if (enemy.FireTimer <= 0 && position.z < 12 && position.z > -10)
                {
                    SpawnBullet(position + new Vector3(0.08f, 0.1f, -1.75f), true);
                    audioSystem.PlayEnemyShot();
                    enemy.FireTimer = UnityEngine.Random.Range(1.5f, 2.7f) / (1 + FlightTime / 80);
                }
                if (HitsPlane(position, player.transform.position, 0.85f)) { EndFlight(); return; }
            }
        }

        private void SpawnBullet(Vector3 position, bool hostile)
        {
            if (bullets.Count >= 180) return;
            Projectile bullet;
            if (idleBullets.Count > 0) bullet = idleBullets.Pop();
            else
            {
                bullet = new Projectile { Visual = CombatVisuals.Create("Pooled cannon tracer", true) };
                bullet.Visual.transform.rotation = Quaternion.Euler(90, 0, 0);
                bullet.Visual.transform.localScale = new Vector3(0.095f, 0.46f, 0.095f);
                var renderer = bullet.Visual.GetComponent<Renderer>();
                renderer.shadowCastingMode = ShadowCastingMode.Off;
                renderer.receiveShadows = false;
            }
            bullet.Hostile = hostile;
            bullet.Speed = hostile ? -11 : 25;
            bullet.Visual.GetComponent<Renderer>().sharedMaterial = hostile ? enemyShotMaterial : shotMaterial;
            bullet.Visual.transform.position = position;
            bullet.Visual.SetActive(true);
            bullets.Add(bullet);
        }

        private void TickBullets(float dt)
        {
            for (int i = bullets.Count - 1; i >= 0; i--)
            {
                var bullet = bullets[i];
                var oldPosition = bullet.Visual.transform.position;
                var position = oldPosition + Vector3.forward * (bullet.Speed * dt);
                bullet.Visual.transform.position = position;
                if (position.z < -17 || position.z > 17) { RecycleBullet(i); continue; }
                if (bullet.Hostile)
                {
                    if (SweptHit(oldPosition, position, player.transform.position))
                    {
                        RecycleBullet(i);
                        EndFlight();
                        return;
                    }
                }
                else
                {
                    for (int j = enemies.Count - 1; j >= 0; j--)
                    {
                        if (!SweptHit(oldPosition, position, enemies[j].Visual.transform.position)) continue;
                        Explode(enemies[j].Visual.transform.position, false);
                        RecycleEnemy(j);
                        RecycleBullet(i);
                        Score += 10;
                        break;
                    }
                }
            }
        }

        private static bool HitsPlane(Vector3 a, Vector3 b, float radius)
        {
            return Mathf.Abs(a.x - b.x) < radius && Mathf.Abs(a.z - b.z) < 1.2f;
        }
        private static bool SweptHit(Vector3 start, Vector3 end, Vector3 plane)
        {
            return Mathf.Abs(end.x - plane.x) < 0.82f &&
                   Mathf.Min(start.z, end.z) <= plane.z + 1.25f && Mathf.Max(start.z, end.z) >= plane.z - 1.25f;
        }

        private void EndFlight()
        {
            if (State != FlightState.Playing) return;
            State = FlightState.GameOver;
            Explode(player.transform.position, true);
            player.SetActive(false);
            if (Score > BestScore)
            {
                BestScore = Score;
                PlayerPrefs.SetInt("FightAgainstIsrael.BestScore", BestScore);
                PlayerPrefs.Save();
            }
            touchFiring = false;
            mouseDragging = false;
            dragFinger = -1;
            audioSystem.SetPlaying(false);
            hud.ShowGameOver(Score, BestScore, FlightTime);
        }

        private void Explode(Vector3 origin, bool playerLost)
        {
            audioSystem.PlayExplosion();
            shake = playerLost ? 0.34f : 0.12f;
            for (int i = 0; i < 22; i++)
            {
                Vector3 velocity = UnityEngine.Random.onUnitSphere * UnityEngine.Random.Range(2, 7);
                velocity.y *= 0.35f;
                AddSpark(origin, velocity, UnityEngine.Random.Range(0.25f, 0.65f), UnityEngine.Random.Range(0.14f, 0.4f), false);
            }
            for (int i = 0; i < 9; i++)
                AddSpark(origin + UnityEngine.Random.insideUnitSphere * 0.4f, new Vector3(UnityEngine.Random.Range(-1, 1), 0.4f, UnityEngine.Random.Range(-1, 1)), UnityEngine.Random.Range(0.9f, 1.5f), UnityEngine.Random.Range(0.4f, 0.8f), true);
        }

        private void AddSpark(Vector3 origin, Vector3 velocity, float duration, float size, bool smoke)
        {
            if (sparks.Count >= 180) return;
            Spark spark;
            if (idleSparks.Count > 0) spark = idleSparks.Pop();
            else
            {
                spark = new Spark { Visual = CombatVisuals.Create("Pooled impact effect", false) };
                spark.Visual.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
            }
            spark.Velocity = velocity;
            spark.Life = spark.Duration = duration;
            spark.Size = size;
            spark.Smoke = smoke;
            spark.Visual.GetComponent<Renderer>().sharedMaterial = smoke ? smokeMaterial : flameMaterial;
            spark.Visual.transform.position = origin;
            spark.Visual.transform.localScale = Vector3.one * size;
            spark.Visual.SetActive(true);
            sparks.Add(spark);
        }

        private void TickSparks(float dt)
        {
            for (int i = sparks.Count - 1; i >= 0; i--)
            {
                var spark = sparks[i];
                spark.Life -= dt;
                if (spark.Life <= 0)
                {
                    spark.Visual.SetActive(false);
                    idleSparks.Push(spark);
                    sparks.RemoveAt(i);
                    continue;
                }
                spark.Visual.transform.position += spark.Velocity * dt;
                float age = 1 - spark.Life / spark.Duration;
                spark.Visual.transform.localScale = Vector3.one * spark.Size * (spark.Smoke ? (1 + age * 2) * Mathf.Sqrt(1 - age) : 1 - age);
            }
        }

        private void RecycleEnemy(int index)
        {
            var enemy = enemies[index];
            enemy.Visual.SetActive(false);
            idleEnemies.Push(enemy);
            enemies.RemoveAt(index);
        }
        private void RecycleBullet(int index)
        {
            var bullet = bullets[index];
            bullet.Visual.SetActive(false);
            idleBullets.Push(bullet);
            bullets.RemoveAt(index);
        }
        private void ClearCombat()
        {
            for (int i = enemies.Count - 1; i >= 0; i--) RecycleEnemy(i);
            for (int i = bullets.Count - 1; i >= 0; i--) RecycleBullet(i);
            for (int i = sparks.Count - 1; i >= 0; i--)
            {
                sparks[i].Visual.SetActive(false);
                idleSparks.Push(sparks[i]);
            }
            sparks.Clear();
        }

        private float checkElapsed;
        private int checkStage;
        private bool checking;
        private void Start()
        {
            checking = Array.IndexOf(Environment.GetCommandLineArgs(), "-smokeTest") >= 0;
            if (checking) StartFlight();
        }
        private void RunAutomatedChecks(float dt)
        {
            if (!checking) return;
            checkElapsed += dt;
            if (checkStage == 0 && checkElapsed > 0.5f)
            {
                SpawnEnemy();
                enemies[enemies.Count - 1].Visual.transform.position = player.transform.position + Vector3.forward * 7;
                enemies[enemies.Count - 1].Speed = 0;
                enemies[enemies.Count - 1].FireTimer = 99;
                touchFiring = true;
                checkStage = 1;
            }
            if (checkStage == 1 && Score >= 10)
            {
                touchFiring = false;
                Debug.Log("SMOKE_PASS scoring " + Score);
                TogglePause();
                if (State != FlightState.Paused) throw new Exception("Pause failed");
                checkStage = 2;
                checkElapsed = 0;
            }
            if (checkStage == 2 && checkElapsed > 0.5f)
            {
                TogglePause();
                SpawnBullet(player.transform.position + Vector3.forward, true);
                checkStage = 3;
            }
            if (checkStage == 3 && State == FlightState.GameOver)
            {
                Debug.Log("SMOKE_PASS game over");
                StartFlight();
                if (Score != 0 || State != FlightState.Playing) throw new Exception("Restart failed");
                Debug.Log("SMOKE_PASS restart");
                checkStage = 4;
                checkElapsed = 0;
            }
            if (checkStage == 4 && checkElapsed > 1)
            {
                Debug.Log("FIGHT_SMOKE_OK");
                checking = false;
                Application.Quit(0);
            }
            if (checkElapsed > 12)
            {
                Debug.LogError("FIGHT_SMOKE_FAILED stage " + checkStage);
                checking = false;
                Application.Quit(3);
            }
        }
    }
}
