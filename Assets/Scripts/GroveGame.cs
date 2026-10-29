using System.Collections.Generic;
using UnityEngine;

namespace GroveSurvivor
{
    public sealed class GroveGame : MonoBehaviour
    {
        public GameScreen Screen { get; private set; }
        public GameScreen SettingsReturn { get; private set; }
        public Difficulty Difficulty { get; set; } = Difficulty.Survivor;
        public HeroStats Hero { get; private set; } = new HeroStats();
        public UpgradeChoice[] Choices { get; private set; }
        public float Elapsed { get; private set; }
        public int Kills { get; private set; }
        public bool QuitRun { get; private set; }
        public float Volume { get; private set; }
        public bool ShowNumbers { get; private set; }
        public bool ShowParticles { get; private set; }
        public float BestTime => PlayerPrefs.GetFloat("Grove.BestTime", 0);
        public int BestKills => PlayerPrefs.GetInt("Grove.BestKills", 0);
        public Transform Player { get; private set; }
        public Camera ViewCamera { get; private set; }
        public ForestArt Art { get; private set; }
        public static string FormatTime(float seconds) => $"{(int)seconds / 60:00}:{(int)seconds % 60:00}";

        sealed class Enemy
        {
            public Transform View;
            public float Health, Speed, Cooldown, Aim, AimAngle;
            public bool Skeleton;
        }
        sealed class Projectile { public Transform View; public Vector3 Velocity; public float Life; }
        sealed class Gem { public Transform View; public int Value; }
        sealed class Effect { public Transform View; public Vector3 Velocity; public float Life; }
        readonly List<Enemy> enemies = new List<Enemy>();
        readonly List<Projectile> arrows = new List<Projectile>();
        readonly List<Gem> gems = new List<Gem>();
        readonly List<Effect> effects = new List<Effect>();
        readonly List<AudioClip> clips = new List<AudioClip>();
        Transform actors;
        GroveUI ui;
        AudioSource sound;
        Mesh slash;
        float spawnTimer, attackTimer, immunity, hudTimer;
        Font font;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Bootstrap()
        {
            if (FindFirstObjectByType<GroveGame>() == null)
                new GameObject("Grove Survivor").AddComponent<GroveGame>();
        }
        void Awake()
        {
            Application.targetFrameRate = 60;
            Art = new ForestArt();
            Volume = PlayerPrefs.GetFloat("Grove.Volume", .35f);
            ShowNumbers = PlayerPrefs.GetInt("Grove.Numbers", 1) != 0;
            ShowParticles = PlayerPrefs.GetInt("Grove.Particles", 1) != 0;
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            Art.CreateForest(transform);
            actors = new GameObject("Actors").transform;
            actors.SetParent(transform);
            Player = Art.Character(transform, true, false);
            slash = Art.SlashMesh(HeroStats.AttackRange);
            ViewCamera = Camera.main;
            if (ViewCamera == null) ViewCamera = new GameObject("Main Camera", typeof(Camera)).GetComponent<Camera>();
            ViewCamera.tag = "MainCamera";
            ViewCamera.orthographic = true;
            ViewCamera.orthographicSize = 13;
            ViewCamera.nearClipPlane = .1f;
            ViewCamera.farClipPlane = 160;
            ViewCamera.backgroundColor = new Color(.07f, .13f, .1f);
            ViewCamera.transform.rotation = Quaternion.Euler(62, 0, 0);
            if (FindFirstObjectByType<AudioListener>() == null) ViewCamera.gameObject.AddComponent<AudioListener>();
            sound = gameObject.AddComponent<AudioSource>(); sound.playOnAwake = false;
            CreateTones();
            RenderSettings.ambientLight = new Color(.48f, .57f, .49f);
            RenderSettings.fog = true; RenderSettings.fogColor = new Color(.12f, .2f, .16f);
            RenderSettings.fogMode = FogMode.Linear; RenderSettings.fogStartDistance = 35; RenderSettings.fogEndDistance = 85;
            var light = new GameObject("Moonlight", typeof(Light)).GetComponent<Light>();
            light.type = LightType.Directional; light.intensity = 1.1f; light.color = new Color(1, .92f, .75f);
            light.shadows = LightShadows.Soft; light.transform.rotation = Quaternion.Euler(50, -30, 0);
            ui = gameObject.AddComponent<GroveUI>(); ui.Initialize(this, font);
            Go(GameScreen.Home);
        }
        public void Go(GameScreen next)
        {
            Screen = next;
            ui.Render(next);
        }
        public void OpenSettings() { SettingsReturn = Screen; Go(GameScreen.Settings); }
        public void SetVolume(float value) { Volume = value; PlayerPrefs.SetFloat("Grove.Volume", value); PlayerPrefs.Save(); }
        public void ToggleNumbers() { ShowNumbers = !ShowNumbers; PlayerPrefs.SetInt("Grove.Numbers", ShowNumbers ? 1 : 0); PlayerPrefs.Save(); ui.Render(Screen); }
        public void ToggleParticles() { ShowParticles = !ShowParticles; PlayerPrefs.SetInt("Grove.Particles", ShowParticles ? 1 : 0); PlayerPrefs.Save(); ui.Render(Screen); }
        public void StartRun()
        {
            foreach (Transform child in actors) Destroy(child.gameObject);
            enemies.Clear(); arrows.Clear(); gems.Clear(); effects.Clear();
            Hero = new HeroStats(); Player.position = Vector3.zero; Player.rotation = Quaternion.identity;
            Elapsed = 0; Kills = 0; QuitRun = false; spawnTimer = .3f; attackTimer = .3f; immunity = 1;
            Go(GameScreen.Playing);
        }
        public void Finish(bool quit)
        {
            if (Screen == GameScreen.Results) return;
            QuitRun = quit;
            PlayerPrefs.SetFloat("Grove.BestTime", Mathf.Max(BestTime, Elapsed));
            PlayerPrefs.SetInt("Grove.BestKills", Mathf.Max(BestKills, Kills));
            PlayerPrefs.Save(); Go(GameScreen.Results);
        }
        public void ChooseUpgrade(int index)
        {
            if (Screen != GameScreen.Upgrade || index < 0 || index >= Choices.Length) return;
            Hero.Apply(Choices[index]); PlayTone(3); Go(GameScreen.Playing); TryUpgrade();
        }
        void TryUpgrade()
        {
            if (Screen != GameScreen.Playing || !Hero.TryLevelUp()) return;
            Choices = UpgradeChoice.Roll(); PlayTone(3); Go(GameScreen.Upgrade);
        }
        void Update()
        {
            if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.P))
            {
                if (Screen == GameScreen.Playing) Go(GameScreen.Paused);
                else if (Screen == GameScreen.Paused) Go(GameScreen.Playing);
                else if (Screen == GameScreen.Settings) Go(SettingsReturn);
            }
            if (Screen == GameScreen.Upgrade)
            {
                if (Input.GetKeyDown(KeyCode.Alpha1)) ChooseUpgrade(0);
                else if (Input.GetKeyDown(KeyCode.Alpha2)) ChooseUpgrade(1);
                else if (Input.GetKeyDown(KeyCode.Alpha3)) ChooseUpgrade(2);
            }
            if (Screen != GameScreen.Playing) return;
            float dt = Mathf.Min(Time.deltaTime, .05f);
            Elapsed += dt; immunity -= dt;
            var move = new Vector3(
                (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow) ? 1 : 0) - (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow) ? 1 : 0), 0,
                (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow) ? 1 : 0) - (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow) ? 1 : 0));
            Player.position += Vector3.ClampMagnitude(move, 1) * HeroStats.MoveSpeed * dt;
            Player.position = new Vector3(Mathf.Clamp(Player.position.x, -78, 78), 0, Mathf.Clamp(Player.position.z, -78, 78));
            if (move.sqrMagnitude > .01f) Player.rotation = Quaternion.LookRotation(move);
            attackTimer -= dt;
            if (attackTimer <= 0) { attackTimer = Hero.AttackInterval; Attack(); }
            var rules = DifficultyRules.Get(Difficulty);
            spawnTimer -= dt;
            if (spawnTimer <= 0)
            {
                spawnTimer = Mathf.Max(.28f, 1.7f - Elapsed * .006f) * rules.SpawnInterval;
                int count = rules.WaveSize + Mathf.FloorToInt(Elapsed / 65);
                for (int i = 0; i < count && enemies.Count < 140; i++) Spawn(rules);
            }
            UpdateEnemies(dt, rules);
            if (Screen != GameScreen.Playing) return;
            UpdateProjectiles(dt, rules);
            if (Screen != GameScreen.Playing) return;
            UpdateGems(dt);
            for (int i = effects.Count - 1; i >= 0; i--)
            {
                var effect = effects[i]; effect.Life -= dt; effect.View.position += effect.Velocity * dt;
                if (effect.Life <= 0) { Destroy(effect.View.gameObject); effects.RemoveAt(i); }
            }
            TryUpgrade();
            hudTimer -= dt;
            if (Screen == GameScreen.Playing && hudTimer <= 0) { ui.UpdateHud(); hudTimer = .1f; }
        }
        void LateUpdate()
        {
            bool menu = Screen == GameScreen.Home || Screen == GameScreen.Selection || Screen == GameScreen.Help ||
                (Screen == GameScreen.Settings && SettingsReturn == GameScreen.Home);
            var target = Player.position + new Vector3(menu ? -6 : 0, 22, -11.7f);
            ViewCamera.transform.position = Vector3.Lerp(ViewCamera.transform.position, target, 1 - Mathf.Exp(-Time.unscaledDeltaTime * 8));
        }
        void OnApplicationFocus(bool focused) { if (!focused && Screen == GameScreen.Playing) Go(GameScreen.Paused); }
        void OnApplicationPause(bool paused) { if (paused && Screen == GameScreen.Playing) Go(GameScreen.Paused); }
        void Spawn(DifficultyRules rules)
        {
            bool skeleton = Elapsed > 8 && Random.value < .28f;
            var view = Art.Character(actors, false, skeleton);
            Vector2 ring = Random.insideUnitCircle.normalized;
            // The radius stays outside the horizontal and vertical orthographic field of view.
            float radius = Mathf.Max(24, ViewCamera.orthographicSize * ViewCamera.aspect + 5);
            view.position = Player.position + new Vector3(ring.x, 0, ring.y) * radius;
            enemies.Add(new Enemy { View = view, Skeleton = skeleton,
                Health = (skeleton ? 30 : 42) * rules.Health * (1 + Elapsed / 130),
                Speed = (skeleton ? 1.7f : Random.Range(1.5f, 2.1f)) * (1 + Mathf.Min(Elapsed / 450, .75f)), Cooldown = Random.Range(1.3f, 2.6f) });
        }
        void Attack()
        {
            Enemy nearest = null; float min = float.MaxValue;
            foreach (var e in enemies)
            {
                float d = (e.View.position - Player.position).sqrMagnitude;
                if (e.Health > 0 && d < min) { nearest = e; min = d; }
            }
            if (nearest != null)
            {
                var dir = nearest.View.position - Player.position;
                if (dir.sqrMagnitude > .001f) Player.rotation = Quaternion.LookRotation(dir);
            }
            var swing = new GameObject("Sword Arc", typeof(MeshFilter), typeof(MeshRenderer));
            swing.transform.SetParent(actors); swing.transform.SetPositionAndRotation(Player.position + Vector3.up * .15f, Player.rotation);
            swing.GetComponent<MeshFilter>().sharedMesh = slash; swing.GetComponent<MeshRenderer>().sharedMaterial = Art.Material(ForestArt.Gold);
            effects.Add(new Effect { View = swing.transform, Life = .18f });
            bool hit = false;
            foreach (var e in enemies)
            {
                Vector3 dir = e.View.position - Player.position;
                if (e.Health <= 0 || dir.magnitude > HeroStats.AttackRange + .4f || Vector3.Angle(Player.forward, dir) > 75) continue;
                e.Health -= Hero.Damage; e.View.position += dir.normalized * .6f;
                Floating(e.View.position, Mathf.RoundToInt(Hero.Damage).ToString(), ForestArt.Gold);
                Burst(e.View.position + Vector3.up, ForestArt.Gold, 4); hit = true;
            }
            if (hit) PlayTone(0);
        }
        void UpdateEnemies(float dt, DifficultyRules rules)
        {
            for (int i = enemies.Count - 1; i >= 0; i--)
            {
                var e = enemies[i];
                if (e.Health <= 0)
                {
                    Kills++;
                    var gem = Art.Shape("Experience Crystal", PrimitiveType.Cube, actors, e.View.position + Vector3.up * .35f, Vector3.one * .26f, new Color(.5f, .9f, .65f)).transform;
                    gem.rotation = Quaternion.Euler(45, 45, 45); gems.Add(new Gem { View = gem, Value = e.Skeleton ? 4 : 3 });
                    Burst(e.View.position + Vector3.up, new Color(.4f, .65f, .44f), 6);
                    Destroy(e.View.gameObject); enemies.RemoveAt(i); continue;
                }
                Vector3 delta = Player.position - e.View.position; delta.y = 0;
                float distance = delta.magnitude; var direction = delta.normalized;
                if (distance > 90)
                {
                    Destroy(e.View.gameObject); enemies.RemoveAt(i); continue;
                }
                if (direction.sqrMagnitude > .001f) e.View.rotation = Quaternion.LookRotation(direction);
                e.Cooldown -= dt;
                if (!e.Skeleton)
                {
                    if (distance > .85f) e.View.position += direction * e.Speed * dt;
                    if (distance < 1.05f) Hurt(10 * rules.Damage);
                }
                else if (e.Aim > 0)
                {
                    e.Aim -= dt;
                    if (e.Aim <= 0)
                    {
                        Vector3 shot = new Vector3(Mathf.Sin(e.AimAngle), 0, Mathf.Cos(e.AimAngle));
                        var arrow = Art.Shape("Skeleton Arrow", PrimitiveType.Cube, actors, e.View.position + Vector3.up * .9f, new Vector3(.07f, .07f, .9f), ForestArt.Gold).transform;
                        arrow.rotation = Quaternion.LookRotation(shot);
                        arrows.Add(new Projectile { View = arrow, Velocity = shot * 9, Life = 6 });
                        e.Cooldown = 2.7f;
                    }
                }
                else
                {
                    float movement = distance > 10 ? 1 : distance < 6 ? -1 : 0;
                    e.View.position += direction * e.Speed * dt * movement;
                    if (e.Cooldown <= 0 && distance < 19)
                    {
                        e.Aim = .65f; e.AimAngle = Mathf.Atan2(direction.x, direction.z);
                        Floating(e.View.position + Vector3.up * .5f, "!", ForestArt.Gold, true);
                    }
                    if (distance < .9f) Hurt(7 * rules.Damage);
                }
                // Separate bodies without a dependency on Unity physics or rigidbodies.
                for (int j = i - 1; j >= 0; j--)
                {
                    var other = enemies[j]; var diff = e.View.position - other.View.position; diff.y = 0;
                    float length = diff.magnitude;
                    if (length > .001f && length < .85f)
                    {
                        var push = diff / length * (.85f - length) * .5f;
                        e.View.position += push; other.View.position -= push;
                    }
                }
                if (Screen != GameScreen.Playing) return;
            }
        }
        void UpdateProjectiles(float dt, DifficultyRules rules)
        {
            for (int i = arrows.Count - 1; i >= 0; i--)
            {
                var a = arrows[i]; a.View.position += a.Velocity * dt; a.Life -= dt;
                var delta = a.View.position - Player.position; delta.y = 0;
                if (delta.sqrMagnitude < .55f * .55f) { Hurt(12 * rules.Damage); a.Life = 0; }
                if (a.Life <= 0) { Destroy(a.View.gameObject); arrows.RemoveAt(i); }
                if (Screen != GameScreen.Playing) return;
            }
        }
        void UpdateGems(float dt)
        {
            for (int i = gems.Count - 1; i >= 0; i--)
            {
                var g = gems[i]; var target = Player.position + Vector3.up * .35f;
                float distance = Vector3.Distance(g.View.position, target);
                g.View.Rotate(0, 90 * dt, 0, Space.World);
                if (distance < 4.7f) g.View.position = Vector3.MoveTowards(g.View.position, target, dt * (distance < 2.4f ? 16 : 9));
                if (distance < .75f)
                { Hero.Experience += g.Value; Destroy(g.View.gameObject); gems.RemoveAt(i); PlayTone(2); }
            }
            if (gems.Count > 400)
            {
                Gem target = null;
                for (int i = gems.Count - 1; i >= 0; i--)
                {
                    if (Vector3.Distance(gems[i].View.position, Player.position) < 12) continue;
                    if (target == null) { target = gems[i]; continue; }
                    target.Value += gems[i].Value; Destroy(gems[i].View.gameObject); gems.RemoveAt(i);
                }
            }
        }
        void Hurt(float damage)
        {
            if (immunity > 0 || Screen != GameScreen.Playing) return;
            Hero.Health = Mathf.Max(0, Hero.Health - damage); immunity = .65f;
            Floating(Player.position, "−" + Mathf.RoundToInt(damage), new Color(1, .5f, .4f));
            Burst(Player.position + Vector3.up, new Color(.8f, .3f, .2f), 8); PlayTone(1);
            if (Hero.Health <= 0) Finish(false);
        }
        void Floating(Vector3 position, string value, Color color, bool required = false)
        {
            if (!ShowNumbers && !required) return;
            var go = new GameObject("Combat Text", typeof(TextMesh)); go.transform.SetParent(actors);
            go.transform.position = position + Vector3.up * 2.3f; go.transform.rotation = ViewCamera.transform.rotation;
            var text = go.GetComponent<TextMesh>(); text.text = value; text.font = font; text.fontSize = 40;
            text.characterSize = .09f; text.anchor = TextAnchor.MiddleCenter; text.color = color;
            go.GetComponent<MeshRenderer>().sharedMaterial = font.material;
            effects.Add(new Effect { View = go.transform, Life = .7f, Velocity = Vector3.up * 1.2f });
        }
        void Burst(Vector3 position, Color color, int count)
        {
            if (!ShowParticles) return;
            for (int i = 0; i < count; i++)
            {
                var particle = Art.Shape("Hit Spark", PrimitiveType.Cube, actors, position, Vector3.one * .075f, color).transform;
                effects.Add(new Effect { View = particle, Life = Random.Range(.15f, .4f), Velocity = Random.insideUnitSphere * 3 });
            }
        }
        void CreateTones()
        {
            foreach (float frequency in new[] { 230f, 100f, 650f, 900f })
            {
                const int samples = 4410;
                var data = new float[samples];
                for (int i = 0; i < samples; i++) data[i] = Mathf.Sin(2 * Mathf.PI * frequency * i / 44100f) * (1 - i / (float)samples) * .18f;
                var clip = AudioClip.Create("Synth " + frequency, samples, 1, 44100, false); clip.SetData(data, 0); clips.Add(clip);
            }
        }
        void PlayTone(int index) { if (Volume > 0) sound.PlayOneShot(clips[index], Volume); }
        void OnDestroy() { if (Art != null) Art.Dispose(); foreach (var clip in clips) Destroy(clip); }
    }
}
