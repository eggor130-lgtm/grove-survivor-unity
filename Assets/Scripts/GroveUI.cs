using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace GroveSurvivor
{
    public sealed class GroveUI : MonoBehaviour
    {
        readonly Color ink = new Color(.06f, .11f, .09f, .96f);
        readonly Color panel = new Color(.1f, .18f, .14f, .97f);
        readonly Color text = new Color(.94f, .92f, .85f);
        readonly Color muted = new Color(.63f, .72f, .65f);
        GroveGame game;
        Font font;
        RectTransform root;
        Text health, level, experience, timer, kills, weapon;
        RectTransform healthFill, xpFill;

        public void Initialize(GroveGame owner, Font runtimeFont)
        {
            game = owner; font = runtimeFont;
            var canvasGo = new GameObject("Game UI", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasGo.transform.SetParent(transform);
            var canvas = canvasGo.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scale = canvasGo.GetComponent<CanvasScaler>();
            scale.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scale.referenceResolution = new Vector2(1600, 900); scale.matchWidthOrHeight = .5f;
            root = canvasGo.GetComponent<RectTransform>();
            if (FindFirstObjectByType<EventSystem>() == null)
                new GameObject("Event System", typeof(EventSystem), typeof(StandaloneInputModule));
        }
        RectTransform Box(Transform parent, string name, float x, float y, float width, float height)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rect = go.GetComponent<RectTransform>(); rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = new Vector2(0, 1); rect.pivot = new Vector2(0, 1);
            rect.anchoredPosition = new Vector2(x, -y); rect.sizeDelta = new Vector2(width, height); return rect;
        }
        RectTransform Center(Transform parent, float width, float height)
        {
            var rect = Box(parent, "Content", 0, 0, width, height);
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(.5f, .5f); rect.anchoredPosition = Vector2.zero; return rect;
        }
        Image Background(RectTransform rect, Color color)
        { var image = rect.gameObject.AddComponent<Image>(); image.color = color; return image; }
        RectTransform Cover(string name, Color color)
        {
            var rect = Box(root, name, 0, 0, 0, 0);
            rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.offsetMin = rect.offsetMax = Vector2.zero;
            Background(rect, color); return rect;
        }
        Text Label(Transform parent, string value, float x, float y, float width, float height, int size = 22, Color? color = null, TextAnchor align = TextAnchor.UpperLeft)
        {
            var rect = Box(parent, "Text " + value, x, y, width, height);
            var label = rect.gameObject.AddComponent<Text>(); label.font = font; label.text = value;
            label.fontSize = size; label.color = color ?? text; label.alignment = align;
            label.raycastTarget = false; label.horizontalOverflow = HorizontalWrapMode.Wrap; label.verticalOverflow = VerticalWrapMode.Truncate;
            return label;
        }
        Button Button(Transform parent, string value, float x, float y, float width, float height, Action action, bool primary = false)
        {
            var rect = Box(parent, value, x, y, width, height);
            var image = Background(rect, primary ? ForestArt.Gold : panel);
            var button = rect.gameObject.AddComponent<Button>(); button.targetGraphic = image;
            var colors = button.colors; colors.highlightedColor = new Color(1.15f, 1.15f, 1.1f); colors.pressedColor = new Color(.75f, .8f, .73f); button.colors = colors;
            button.navigation = new Navigation { mode = Navigation.Mode.None };
            var outline = rect.gameObject.AddComponent<Outline>(); outline.effectColor = primary ? ForestArt.Gold : new Color(.35f, .47f, .36f, .45f); outline.effectDistance = Vector2.one;
            Label(rect, value, 16, 0, width - 32, height, 22, primary ? ink : text, TextAnchor.MiddleCenter);
            button.onClick.AddListener(() => { EventSystem.current.SetSelectedGameObject(null); action(); }); return button;
        }
        void Header()
        {
            var header = Box(root, "Header", 0, 0, 0, 90); header.anchorMax = new Vector2(1, 1);
            Background(header, new Color(.04f, .09f, .07f, .8f));
            Label(header, "СУМРАЧНЫЙ ЛЕС", 50, 28, 600, 45, 24, ForestArt.Gold);
            var badge = Label(header, "UNITY / ПРОТОТИП 01", 0, 30, 350, 40, 16, muted, TextAnchor.UpperRight).rectTransform;
            badge.anchorMin = badge.anchorMax = new Vector2(1, 1); badge.pivot = new Vector2(1, 1); badge.anchoredPosition = new Vector2(-50, -30);
        }
        void Eyebrow(Transform parent, string value, float y = 0) { Label(parent, value.ToUpperInvariant(), 0, y, 1100, 30, 16, ForestArt.Gold); }
        void Title(Transform parent, string value, float y = 36) { Label(parent, value, 0, y, 1100, 85, 48); }
        public void Render(GameScreen screen)
        {
            foreach (Transform child in root) { child.gameObject.SetActive(false); Destroy(child.gameObject); }
            switch (screen)
            {
                case GameScreen.Home: Home(); break;
                case GameScreen.Selection: Selection(); break;
                case GameScreen.Settings: Settings(); break;
                case GameScreen.Help: Help(); break;
                case GameScreen.Playing: Hud(); break;
                case GameScreen.Paused: Pause(); break;
                case GameScreen.Upgrade: Upgrade(); break;
                case GameScreen.Results: Results(); break;
            }
        }
        void Home()
        {
            var shade = Cover("Menu Shade", new Color(.025f, .07f, .04f, .63f)); Header();
            var content = Center(shade, 1260, 620);
            Eyebrow(content, "Последний свет среди теней");
            Label(content, "Лес помнит.\nВыживи.", 0, 48, 800, 205, 79, text);
            Label(content, "Один меч. Бесконечная орда.\nВойди в лес, стань сильнее и продержись ещё немного.", 0, 265, 570, 85, 23, muted);
            Button(content, "Начать игру   →", 0, 375, 390, 66, () => game.Go(GameScreen.Selection), true);
            Button(content, "Настройки", 0, 457, 390, 60, game.OpenSettings);
            Button(content, "Как играть", 0, 533, 390, 60, () => game.Go(GameScreen.Help));
            Label(content, "РЕКОРД ВЫЖИВАНИЯ\n" + GroveGame.FormatTime(game.BestTime), 780, 485, 330, 85, 24, ForestArt.Gold);
            Label(content, "ПОБЕЖДЕНО: " + game.BestKills, 780, 584, 330, 40, 18, muted);
            Button(content, "Выход", 1110, 570, 150, 50, Quit);
        }
        void Selection()
        {
            var shade = Cover("Selection Shade", new Color(.025f, .07f, .04f, .88f)); Header();
            var content = Center(shade, 1130, 680);
            Button(content, "← Главное меню", 0, 0, 255, 45, () => game.Go(GameScreen.Home));
            Eyebrow(content, "Перед рассветом", 70); Title(content, "Выбери свой путь", 108);
            Label(content, "01 / ПЕРСОНАЖ", 0, 205, 480, 30, 16, muted);
            var hero = Box(content, "Swordsman Card", 0, 245, 510, 310); Background(hero, panel);
            Label(hero, "МЕЧНИК", 28, 28, 450, 65, 44, ForestArt.Gold);
            Label(hero, "Сталь, выдержка и ни шага назад.\nШирокая автоматическая атака мечом.\n\nЗдоровье: 100   ·   Урон: 24\nУдар каждые 0,85 секунды", 28, 110, 460, 135, 23, text);
            Button(hero, "✓ Выбран · доступен всегда", 25, 252, 460, 40, () => { }, true);
            Label(content, "02 / СЛОЖНОСТЬ", 560, 205, 540, 30, 16, muted);
            for (int i = 0; i < 3; i++)
            {
                var difficulty = (Difficulty)i; var rules = DifficultyRules.Get(difficulty);
                string value = (game.Difficulty == difficulty ? "✓  " : "") + rules.Name +
                    $"\nHP ×{rules.Health:0.##}  ·  {rules.WaveSize} за волну  ·  спавн ×{rules.SpawnInterval:0.##}";
                Button(content, value, 560, 245 + i * 105, 570, 94, () => { game.Difficulty = difficulty; Render(GameScreen.Selection); }, game.Difficulty == difficulty);
            }
            Label(content, "WASD / стрелки — движение\nАтака автоматическая · Бесконечное выживание", 0, 595, 670, 70, 20, muted);
            Button(content, "Войти в лес   →", 770, 602, 360, 65, game.StartRun, true);
        }
        void Settings()
        {
            var shade = Cover("Settings Shade", ink); Header(); var content = Center(shade, 900, 630);
            Button(content, "← Назад", 0, 0, 200, 45, () => game.Go(game.SettingsReturn));
            Eyebrow(content, "Под себя", 80); Title(content, "Настройки", 117);
            Label(content, "Громкость эффектов", 0, 245, 500, 45, 26);
            CreateSlider(content, 560, 245, game.Volume, game.SetVolume);
            Label(content, "Частицы попаданий", 0, 332, 500, 45, 26);
            Button(content, game.ShowParticles ? "Включены" : "Выключены", 560, 325, 320, 55, game.ToggleParticles);
            Label(content, "Числа урона", 0, 415, 500, 45, 26);
            Button(content, game.ShowNumbers ? "Включены" : "Выключены", 560, 408, 320, 55, game.ToggleNumbers);
            Label(content, "Полноэкранный режим", 0, 498, 510, 45, 26);
            Button fullscreenButton = null;
            fullscreenButton = Button(content, UnityEngine.Screen.fullScreen ? "Оконный режим" : "На весь экран", 560, 491, 320, 55,
                () => {
                    bool next = !UnityEngine.Screen.fullScreen;
                    UnityEngine.Screen.fullScreen = next;
                    fullscreenButton.GetComponentInChildren<Text>().text = next ? "Оконный режим" : "На весь экран";
                });
            Label(content, "Громкость и эффекты сохраняются автоматически на этом компьютере.", 0, 589, 900, 50, 18, muted);
        }
        void CreateSlider(Transform parent, float x, float y, float value, Action<float> changed)
        {
            var rect = Box(parent, "Volume", x, y, 320, 42);
            var track = Box(rect, "Track", 0, 17, 320, 8); Background(track, new Color(.3f, .4f, .32f));
            var handleArea = Box(rect, "Handle Area", 10, 0, 300, 42);
            var handle = Box(handleArea, "Handle", 0, 0, 22, 42); handle.pivot = new Vector2(.5f, .5f);
            var graphic = Background(handle, ForestArt.Gold);
            var slider = rect.gameObject.AddComponent<Slider>(); slider.minValue = 0; slider.maxValue = 1;
            slider.handleRect = handle; slider.targetGraphic = graphic; slider.direction = Slider.Direction.LeftToRight;
            slider.SetValueWithoutNotify(value);
            slider.onValueChanged.AddListener(v => changed(v));
        }
        void Help()
        {
            var shade = Cover("Help Shade", ink); Header(); var content = Center(shade, 1030, 650);
            Button(content, "← Главное меню", 0, 0, 255, 45, () => game.Go(GameScreen.Home));
            Eyebrow(content, "Искусство выживать", 75); Title(content, "Пусть меч говорит", 115);
            Label(content,
                "WASD / стрелки — передвижение.     Esc / P — пауза.\n\n" +
                "Мечник сам поворачивается к ближайшему врагу и бьёт по широкой дуге.\nТвоя задача — двигаться и выбирать маршрут.\n\n" +
                "Зомби атакуют вблизи. Скелеты держат дистанцию и стреляют.\nЗнак ! предупреждает о выстреле: смени направление.\n\n" +
                "Собирай зелёные кристаллы опыта. На новом уровне выбери одно из трёх\nулучшений кнопкой или клавишами 1 / 2 / 3. Время выбора не идёт.\n\n" +
                "С каждым новым забегом характеристики сбрасываются. Рекорды остаются.\nЛесная поляна ограничена по краям; враги постепенно становятся сильнее.",
                0, 220, 1030, 430, 24, muted);
        }
        RectTransform Bar(Transform parent, float x, float y, float width, Color color)
        {
            var rect = Box(parent, "Bar", x, y, width, 8); Background(rect, new Color(.25f, .3f, .25f));
            var fill = Box(rect, "Fill", 0, 0, 0, 0);
            fill.anchorMin = Vector2.zero; fill.anchorMax = Vector2.one; fill.offsetMin = fill.offsetMax = Vector2.zero;
            Background(fill, color); return fill;
        }
        void Hud()
        {
            var top = Box(root, "HUD", 0, 0, 0, 110); top.anchorMax = new Vector2(1, 1);
            Background(top, new Color(.035f, .08f, .05f, .86f));
            health = Label(top, "", 35, 19, 370, 30, 20);
            healthFill = Bar(top, 35, 57, 360, new Color(.77f, .43f, .34f));
            level = Label(top, "", 35, 78, 210, 26, 16, ForestArt.Gold);
            experience = Label(top, "", 240, 78, 155, 26, 16, muted, TextAnchor.UpperRight);
            var mid = Center(top, 360, 100);
            timer = Label(mid, "", 0, 8, 360, 56, 40, text, TextAnchor.MiddleCenter);
            Label(mid, DifficultyRules.Get(game.Difficulty).Name.ToUpperInvariant(), 0, 69, 360, 30, 16, ForestArt.Gold, TextAnchor.MiddleCenter);
            var right = Box(top, "Right", 0, 15, 370, 85);
            right.anchorMin = right.anchorMax = new Vector2(1, 1); right.pivot = new Vector2(1, 1); right.anchoredPosition = new Vector2(-30, -15);
            kills = Label(right, "", 0, 5, 250, 70, 23, text, TextAnchor.MiddleRight);
            Button(right, "II", 280, 5, 85, 65, () => game.Go(GameScreen.Paused));
            var bottom = Box(root, "Bottom HUD", 0, 0, 0, 88);
            bottom.anchorMin = Vector2.zero; bottom.anchorMax = new Vector2(1, 0); bottom.pivot = Vector2.zero;
            Background(bottom, new Color(.035f, .08f, .05f, .82f));
            weapon = Label(bottom, "", 35, 18, 850, 52, 20, ForestArt.Gold);
            Label(bottom, "WASD — движение  ·  ESC — пауза", 0, 0, 470, 75, 19, muted, TextAnchor.MiddleRight).rectTransform.SetInsetAndSizeFromParentEdge(RectTransform.Edge.Right, 35, 470);
            xpFill = Bar(bottom, 0, 0, 1600, ForestArt.Gold);
            var xpTrack = (RectTransform)xpFill.parent; xpTrack.anchorMax = new Vector2(1, 1); xpTrack.sizeDelta = new Vector2(0, 6);
            UpdateHud();
        }
        public void UpdateHud()
        {
            var hero = game.Hero;
            health.text = $"МЕЧНИК     {Mathf.CeilToInt(hero.Health)} / {hero.MaxHealth:0}";
            healthFill.anchorMax = new Vector2(hero.Health / hero.MaxHealth, 1);
            level.text = "УРОВЕНЬ " + hero.Level;
            experience.text = $"{hero.Experience} / {hero.RequiredExperience} XP";
            xpFill.anchorMax = new Vector2(hero.Experience / (float)hero.RequiredExperience, 1);
            timer.text = GroveGame.FormatTime(game.Elapsed); kills.text = $"{game.Kills} ПОБЕЖДЕНО";
            weapon.text = $"СТАЛЬНОЙ МЕЧ\n{hero.Damage:0} урона  ·  {hero.AttackInterval:0.00} с";
        }
        void Pause()
        {
            var shade = Cover("Pause Overlay", new Color(.025f, .06f, .04f, .92f));
            var content = Center(shade, 1050, 310);
            Eyebrow(content, "Тишина перед бурей"); Title(content, "Передохни, путник");
            Label(content, $"{GroveGame.FormatTime(game.Elapsed)} в лесу  ·  Уровень {game.Hero.Level}  ·  {game.Kills} побеждено", 0, 135, 1050, 60, 24, muted);
            Button(content, "Продолжить", 0, 230, 320, 65, () => game.Go(GameScreen.Playing), true);
            Button(content, "Настройки", 345, 230, 300, 65, game.OpenSettings);
            Button(content, "Завершить забег", 670, 230, 350, 65, () => game.Finish(true));
        }
        void Upgrade()
        {
            var shade = Cover("Upgrade Overlay", new Color(.025f, .06f, .04f, .94f));
            var content = Center(shade, 1180, 490);
            Eyebrow(content, "Уровень " + game.Hero.Level); Title(content, "Лес делает тебя сильнее");
            Label(content, "Выбери одно улучшение. Время остановлено.", 0, 140, 1180, 50, 23, muted);
            for (int i = 0; i < game.Choices.Length; i++)
            {
                int index = i; var choice = game.Choices[i];
                var card = Button(content, "", i * 400, 230, 380, 250, () => game.ChooseUpgrade(index));
                Label(card.transform, choice.Name, 25, 30, 330, 65, 28, ForestArt.Gold);
                Label(card.transform, choice.Description + "\n" + choice.Bonus, 25, 112, 330, 85, 25);
                Label(card.transform, $"{i + 1}  /  ВЫБРАТЬ", 25, 209, 330, 30, 16, muted);
            }
        }
        void Results()
        {
            var shade = Cover("Results Overlay", new Color(.025f, .06f, .04f, .95f));
            var content = Center(shade, 1120, 420);
            Eyebrow(content, "Лес забрал своё"); Title(content, "Но сталь помнит.");
            Label(content, game.QuitRun ? "Забег завершён." : "Мечник пал. Следующий забег станет твоим.", 0, 133, 1120, 55, 24, muted);
            Label(content, $"ВЫЖИВАНИЕ\n{GroveGame.FormatTime(game.Elapsed)}", 0, 214, 370, 90, 30, ForestArt.Gold);
            Label(content, "ПОБЕЖДЕНО\n" + game.Kills, 400, 214, 350, 90, 30, ForestArt.Gold);
            Label(content, "УРОВЕНЬ\n" + game.Hero.Level, 800, 214, 320, 90, 30, ForestArt.Gold);
            Button(content, "Ещё один забег", 0, 345, 350, 65, game.StartRun, true);
            Button(content, "Выбор сложности", 385, 345, 350, 65, () => game.Go(GameScreen.Selection));
            Button(content, "Главное меню", 770, 345, 350, 65, () => game.Go(GameScreen.Home));
        }
        static void Quit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
