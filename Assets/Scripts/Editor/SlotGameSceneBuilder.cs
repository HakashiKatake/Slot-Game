using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using TMPro;
using SlotGame.Core;
using SlotGame.Reels;
using SlotGame.UI;
using SlotGame.Audio;
using SlotGame.Controllers;

namespace SlotGame.Editor
{
    public static class SlotGameSceneBuilder
    {
        private const string ScenePath = "Assets/Scenes/SampleScene.unity";
        private const string SettingsFolder = "Assets/Settings";

        [MenuItem("Tools/Slot Game/Setup Complete Scene")]
        public static void BuildCompleteSlotScene()
        {
            try
            {
                EditorUtility.DisplayProgressBar("Setting up Slot Game", "Creating Scriptable Objects...", 0.1f);
                EnsureDirectoryExists(SettingsFolder);

                // 1. Load Sprite References
                var sevenSprite = LoadSubSprite("Assets/Sprites/slot-symbol1.png", "slot-symbol1_0");
                var cherrySprite = LoadSubSprite("Assets/Sprites/slot-symbol2.png", "slot-symbol2_0");
                var bellSprite = LoadSubSprite("Assets/Sprites/slot-symbol3.png", "slot-symbol3_0");
                var barSprite = LoadSubSprite("Assets/Sprites/slot-symbol4.png", "slot-symbol4_0");

                var cabinetSprite = LoadSubSprite("Assets/Sprites/slot-machine1.png", "slot-machine1_0");
                var cabinetOverlaySprite = LoadSubSprite("Assets/Sprites/slot-machine4.png", "slot-machine4_0");
                var leverUpSprite = LoadSubSprite("Assets/Sprites/slot-machine2.png", "slot-machine2_0");
                var leverDownSprite = LoadSubSprite("Assets/Sprites/slot-machine-3.png", "slot-machine-3_0");
                var glass0 = LoadSubSprite("Assets/Sprites/slot-machine5.png", "slot-machine5_0");
                var glass1 = LoadSubSprite("Assets/Sprites/slot-machine5.png", "slot-machine5_1");
                var glass2 = LoadSubSprite("Assets/Sprites/slot-machine5.png", "slot-machine5_2");
                var closeBtnSprite = LoadSubSprite("Assets/Sprites/slot_machine_buttons-02.png", "slot_machine_buttons-02_0");
                var yesBtnSprite = LoadSubSprite("Assets/Sprites/Yes_No_Btn.png", "Yes_No_Btn_0");
                var popupBgSprite = LoadSubSprite("Assets/Sprites/popup.png", "popup_0");

                var tavernBgSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/tavern_bg.png");
                var retroMenuSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/retro_menu_frame.png");

                // 2. Load Audio Clips
                var clickClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Sounds/button_click.wav");
                var leverClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Sounds/lever_pull.wav");
                var spinClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Sounds/spin_loop.wav");
                var stopClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Sounds/reel_stop.wav");
                var winClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Sounds/win_bell.wav");
                var jackpotClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Sounds/jackpot_fanfare.wav");

                // 3. Create or Load ScriptableObjects
                var sevenData = CreateOrUpdateSymbolData("SymbolData_Seven", SymbolType.Seven, "Seven", sevenSprite, new Color(1f, 0.25f, 0.2f), 8);
                var bellData = CreateOrUpdateSymbolData("SymbolData_Bell", SymbolType.Bell, "Bell", bellSprite, new Color(1f, 0.85f, 0.15f), 12);
                var barData = CreateOrUpdateSymbolData("SymbolData_Bar", SymbolType.Bar, "Bar", barSprite, new Color(0.35f, 0.75f, 1f), 16);
                var cherryData = CreateOrUpdateSymbolData("SymbolData_Cherry", SymbolType.Cherry, "Cherry", cherrySprite, new Color(1f, 0.2f, 0.45f), 24);

                var symbolList = new List<SymbolDataSO> { sevenData, bellData, barData, cherryData };

                var payoutTable = CreateOrUpdatePayoutTable();
                var gameConfig = CreateOrUpdateGameConfig();

                AssetDatabase.SaveAssets();

                // 4. Load Font Asset
                var fontAsset = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset");

                // 5. Open / Create Scene
                EditorUtility.DisplayProgressBar("Setting up Slot Game", "Building UI & Scene Hierarchy...", 0.4f);
                var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

                // Main Camera
                var camGo = new GameObject("Main Camera");
                camGo.transform.position = new Vector3(0f, 0f, -10f);
                var cam = camGo.AddComponent<Camera>();
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = Color.black;
                cam.orthographic = true;
                cam.orthographicSize = 3.12f;
                cam.nearClipPlane = 0.1f;
                cam.farClipPlane = 100f;
                camGo.tag = "MainCamera";

                // Global Light 2D
                var lightGo = new GameObject("Global Light 2D");
                var lightType = Type.GetType("UnityEngine.Rendering.Universal.Light2D, Unity.RenderPipelines.Universal.Runtime");
                if (lightType != null) lightGo.AddComponent(lightType);

                // EventSystem
                var eventSystemGo = new GameObject("EventSystem");
                eventSystemGo.AddComponent<UnityEngine.EventSystems.EventSystem>();
                var inputModuleType = Type.GetType("UnityEngine.InputSystem.UI.InputSystemUIInputModule, Unity.InputSystem");
                if (inputModuleType != null)
                {
                    eventSystemGo.AddComponent(inputModuleType);
                }
                else
                {
                    eventSystemGo.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
                }

                // Canvas - Native 816 x 624 matching all sprites exactly
                var canvasGo = new GameObject("Canvas");
                var canvas = canvasGo.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceCamera;
                canvas.worldCamera = cam;
                canvas.planeDistance = 5f;
                var scaler = canvasGo.AddComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(816, 624);
                scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
                scaler.matchWidthOrHeight = 0.5f;
                canvasGo.AddComponent<GraphicRaycaster>();

                // Background - Tavern Room from reference
                var bgGo = CreateImageObject("Background", canvasGo.transform, tavernBgSprite);
                var bgRect = bgGo.GetComponent<RectTransform>();
                bgRect.anchorMin = Vector2.zero;
                bgRect.anchorMax = Vector2.one;
                bgRect.offsetMin = Vector2.zero;
                bgRect.offsetMax = Vector2.zero;

                // GameRoot
                var gameRoot = CreateEmptyRect("GameRoot", canvasGo.transform, new Vector2(816, 624));
                gameRoot.anchoredPosition = Vector2.zero;

                // Presenter & Audio Service on GameRoot
                var presenter = gameRoot.gameObject.AddComponent<SlotMachinePresenter>();
                var audioService = gameRoot.gameObject.AddComponent<SlotAudioService>();

                // Audio Sources
                var sfxSource = gameRoot.gameObject.AddComponent<AudioSource>();
                sfxSource.playOnAwake = false;
                var loopSource = gameRoot.gameObject.AddComponent<AudioSource>();
                loopSource.playOnAwake = false;
                loopSource.loop = true;

                // Wire AudioService via SerializedObject
                var audioSo = new SerializedObject(audioService);
                audioSo.FindProperty("sfxSource").objectReferenceValue = sfxSource;
                audioSo.FindProperty("loopSource").objectReferenceValue = loopSource;
                audioSo.FindProperty("buttonClickClip").objectReferenceValue = clickClip;
                audioSo.FindProperty("leverPullClip").objectReferenceValue = leverClip;
                audioSo.FindProperty("spinLoopClip").objectReferenceValue = spinClip;
                audioSo.FindProperty("reelStopClip").objectReferenceValue = stopClip;
                audioSo.FindProperty("winBellClip").objectReferenceValue = winClip;
                audioSo.FindProperty("jackpotFanfareClip").objectReferenceValue = jackpotClip;
                audioSo.ApplyModifiedProperties();

                // ------------------ Cabinet (816 x 624 centered at 0, 0) ------------------
                var cabinetGo = CreateEmptyRect("Cabinet", gameRoot, new Vector2(816, 624));
                cabinetGo.anchoredPosition = Vector2.zero;
                var machineView = cabinetGo.gameObject.AddComponent<SlotMachineView>();

                // 1. Cabinet Body Back
                var cabinetBody = CreateImageObject("CabinetBody", cabinetGo, cabinetSprite);
                var cabinetBodyRect = cabinetBody.GetComponent<RectTransform>();
                cabinetBodyRect.sizeDelta = new Vector2(816, 624);
                cabinetBodyRect.anchoredPosition = Vector2.zero;

                // 2. Reels Container
                var reelsContainer = CreateEmptyRect("ReelWindowsContainer", cabinetGo, new Vector2(816, 624));
                reelsContainer.anchoredPosition = Vector2.zero;
                var reelsManager = reelsContainer.gameObject.AddComponent<ReelsManager>();

                var reelControllers = new List<ReelController>();
                float[] reelXPositions = new float[] { -125.5f, 4.5f, 134.5f };
                Sprite[] glassSprites = new Sprite[] { glass0, glass1, glass2 };

                for (int r = 0; r < 3; r++)
                {
                    var reelColumn = CreateEmptyRect($"Reel_{r}", reelsContainer, new Vector2(108, 210));
                    reelColumn.anchoredPosition = new Vector2(reelXPositions[r], -38.5f);
                    reelColumn.gameObject.AddComponent<RectMask2D>();

                    // Glass backdrop
                    var glassBg = CreateImageObject("GlassBackdrop", reelColumn, glassSprites[r]);
                    var glassRect = glassBg.GetComponent<RectTransform>();
                    glassRect.sizeDelta = new Vector2(108, 210);
                    glassRect.anchoredPosition = Vector2.zero;

                    // Reel Strip
                    var reelStrip = CreateEmptyRect("ReelStrip", reelColumn, new Vector2(108, 450));
                    reelStrip.anchoredPosition = Vector2.zero;
                    var reelCtrl = reelStrip.gameObject.AddComponent<ReelController>();
                    reelControllers.Add(reelCtrl);

                    var cells = new List<SymbolCellView>();
                    for (int c = 0; c < 5; c++)
                    {
                        var cellGo = CreateEmptyRect($"Cell_{c}", reelStrip, new Vector2(96, 70));
                        var cellImg = cellGo.gameObject.AddComponent<Image>();
                        cellImg.preserveAspect = true;
                        var cellView = cellGo.gameObject.AddComponent<SymbolCellView>();

                        var cellSo = new SerializedObject(cellView);
                        cellSo.FindProperty("symbolImage").objectReferenceValue = cellImg;
                        cellSo.FindProperty("rectTransform").objectReferenceValue = cellGo;
                        cellSo.ApplyModifiedProperties();

                        cells.Add(cellView);
                    }

                    var reelSo = new SerializedObject(reelCtrl);
                    reelSo.FindProperty("reelIndex").intValue = r;
                    reelSo.FindProperty("reelContainer").objectReferenceValue = reelStrip;
                    var cellsProp = reelSo.FindProperty("cells");
                    cellsProp.arraySize = cells.Count;
                    for (int i = 0; i < cells.Count; i++)
                    {
                        cellsProp.GetArrayElementAtIndex(i).objectReferenceValue = cells[i];
                    }
                    reelSo.ApplyModifiedProperties();
                }

                // 3. Front Cabinet Overlay (frames the reels with cutouts)
                var frontOverlay = CreateImageObject("CabinetFrontOverlay", cabinetGo, cabinetOverlaySprite);
                var frontRect = frontOverlay.GetComponent<RectTransform>();
                frontRect.sizeDelta = new Vector2(816, 624);
                frontRect.anchoredPosition = Vector2.zero;
                frontOverlay.raycastTarget = false;

                // Wire ReelsManager
                var rmSo = new SerializedObject(reelsManager);
                var rmReelsProp = rmSo.FindProperty("reels");
                rmReelsProp.arraySize = reelControllers.Count;
                for (int i = 0; i < reelControllers.Count; i++)
                {
                    rmReelsProp.GetArrayElementAtIndex(i).objectReferenceValue = reelControllers[i];
                }
                rmSo.ApplyModifiedProperties();

                // 4. Interactive Lever Layer (816 x 624)
                var leverGo = CreateEmptyRect("Lever", cabinetGo, new Vector2(816, 624));
                leverGo.anchoredPosition = Vector2.zero;
                var leverImg = leverGo.gameObject.AddComponent<Image>();
                leverImg.sprite = leverUpSprite;
                leverImg.raycastTarget = false;

                // Lever Click Target (transparent button over lever handle)
                var leverClickGo = CreateEmptyRect("LeverClickZone", cabinetGo, new Vector2(90, 260));
                leverClickGo.anchoredPosition = new Vector2(320f, -50f);
                var leverClickImg = leverClickGo.gameObject.AddComponent<Image>();
                leverClickImg.color = new Color(0, 0, 0, 0);
                var leverBtn = leverClickGo.gameObject.AddComponent<Button>();

                // Wire MachineView
                var mvSo = new SerializedObject(machineView);
                mvSo.FindProperty("cabinetImage").objectReferenceValue = cabinetBody;
                mvSo.FindProperty("leverTransform").objectReferenceValue = leverGo;
                mvSo.FindProperty("leverImage").objectReferenceValue = leverImg;
                mvSo.FindProperty("leverUpSprite").objectReferenceValue = leverUpSprite;
                mvSo.FindProperty("leverDownSprite").objectReferenceValue = leverDownSprite;
                mvSo.ApplyModifiedProperties();

                // ------------------ HUD / UI ------------------
                var hudGo = CreateEmptyRect("HUD", gameRoot, new Vector2(816, 624));
                hudGo.anchoredPosition = Vector2.zero;
                var uiView = hudGo.gameObject.AddComponent<SlotUIView>();

                // 1. Top-Left Credits Display (Pixel retro font matching reference "50")
                var cornerBalGo = CreateEmptyRect("CornerBalance", hudGo, new Vector2(180, 50));
                cornerBalGo.anchoredPosition = new Vector2(-340f, 275f);
                var cornerBalText = CreateTextMesh("BalanceText", cornerBalGo, "1,000G", fontAsset, 30, FontStyles.Bold, Color.white);
                cornerBalText.alignment = TextAlignmentOptions.Left;

                // 2. Status / Win Readout on Machine Shelf Tray (y = -180)
                var statusBannerGo = CreateEmptyRect("StatusBanner", hudGo, new Vector2(400, 36));
                statusBannerGo.anchoredPosition = new Vector2(0f, -180f);
                var statusText = CreateTextMesh("StatusText", statusBannerGo, "PULL LEVER OR SELECT BET!", fontAsset, 16, FontStyles.Bold, new Color(1f, 0.88f, 0.25f));
                statusText.alignment = TextAlignmentOptions.Center;

                // 3. Free Spins Banner (shown during free spins at top)
                var fsBannerGo = CreateEmptyRect("FreeSpinsBanner", hudGo, new Vector2(500, 30));
                fsBannerGo.anchoredPosition = new Vector2(0f, 290f);
                var fsBg = fsBannerGo.gameObject.AddComponent<Image>();
                fsBg.color = new Color(0.45f, 0.1f, 0.65f, 0.95f);
                var fsText = CreateTextMesh("FreeSpinsText", fsBannerGo, "FREE SPINS: 5 (2x WIN MULTIPLIER!)", fontAsset, 15, FontStyles.Bold, Color.white);

                // 4. Retro Quick Bet Menu (matching reference media_1788848596306.png)
                var menuGo = CreateImageObject("RetroQuickBetMenu", hudGo, retroMenuSprite);
                var menuRect = menuGo.GetComponent<RectTransform>();
                menuRect.sizeDelta = new Vector2(150, 198);
                menuRect.anchoredPosition = new Vector2(324.5f, 37.5f);

                // 4 menu option buttons
                var btn10 = CreateRetroMenuButton("Bet10Btn", menuRect, "Bet <color=#F5D46A>10G</color>", fontAsset, new Vector2(0f, 60f), new Vector2(132, 36));
                var btn50 = CreateRetroMenuButton("Bet50Btn", menuRect, "Bet <color=#F5D46A>50G</color>", fontAsset, new Vector2(0f, 20f), new Vector2(132, 36));
                var btn100 = CreateRetroMenuButton("Bet100Btn", menuRect, "Bet <color=#F5D46A>100G</color>", fontAsset, new Vector2(0f, -20f), new Vector2(132, 36));
                var btnExit = CreateRetroMenuButton("ExitBtn", menuRect, "Exit", fontAsset, new Vector2(0f, -60f), new Vector2(132, 36));

                // 5. Utility Buttons: Rules & Sound in discreet bottom corners
                var rulesBtn = CreateRetroMenuButton("RulesBtn", hudGo, "RULES", fontAsset, new Vector2(350f, -280f), new Vector2(80, 30), 12);
                var soundBtn = CreateRetroMenuButton("SoundBtn", hudGo, "SOUND: ON", fontAsset, new Vector2(-350f, -280f), new Vector2(88, 30), 11);
                var reloadBtn = CreateRetroMenuButton("ReloadBtn", hudGo, "+1000G", fontAsset, new Vector2(-350f, 220f), new Vector2(80, 26), 11);

                // ------------------ Win Celebration Popup ------------------
                var winPopupGo = CreateEmptyRect("WinCelebrationPopup", canvasGo.transform, new Vector2(816, 624));
                winPopupGo.anchoredPosition = Vector2.zero;
                var winPopupOverlay = winPopupGo.gameObject.AddComponent<Image>();
                winPopupOverlay.color = new Color(0f, 0f, 0f, 0.75f);

                var winPopupCard = CreateImageObject("Card", winPopupGo, popupBgSprite);
                var winCardRect = winPopupCard.GetComponent<RectTransform>();
                winCardRect.sizeDelta = new Vector2(560, 320);
                winCardRect.anchoredPosition = Vector2.zero;

                var winTitleText = CreateTextMesh("WinTitle", winCardRect, "JACKPOT!", fontAsset, 32, FontStyles.Bold, new Color(1f, 0.85f, 0.2f));
                winTitleText.rectTransform.anchoredPosition = new Vector2(0f, 75f);

                var winAmountText = CreateTextMesh("WinAmount", winCardRect, "+5,000 CREDITS", fontAsset, 34, FontStyles.Bold, new Color(0.35f, 1f, 0.45f));
                winAmountText.rectTransform.anchoredPosition = new Vector2(0f, 20f);

                var winDescText = CreateTextMesh("WinDesc", winCardRect, "3x SEVENS! CONGRATULATIONS!", fontAsset, 16, FontStyles.Normal, Color.white);
                winDescText.rectTransform.anchoredPosition = new Vector2(0f, -32f);

                var winCollectBtnGo = CreateEmptyRect("CollectButton", winCardRect, new Vector2(150, 50));
                winCollectBtnGo.anchoredPosition = new Vector2(0f, -95f);
                var winCollectBtnImg = winCollectBtnGo.gameObject.AddComponent<Image>();
                winCollectBtnImg.sprite = yesBtnSprite;
                var winCollectBtn = winCollectBtnGo.gameObject.AddComponent<Button>();

                // ------------------ Paytable Dialog ------------------
                var paytablePopupGo = CreateEmptyRect("PaytablePopup", canvasGo.transform, new Vector2(816, 624));
                paytablePopupGo.anchoredPosition = Vector2.zero;
                var paytableOverlay = paytablePopupGo.gameObject.AddComponent<Image>();
                paytableOverlay.color = new Color(0f, 0f, 0f, 0.75f);

                var paytableCard = CreateImageObject("Card", paytablePopupGo, popupBgSprite);
                var paytableCardRect = paytableCard.GetComponent<RectTransform>();
                paytableCardRect.sizeDelta = new Vector2(620, 420);
                paytableCardRect.anchoredPosition = Vector2.zero;

                var ptTitle = CreateTextMesh("Title", paytableCardRect, "PAYOUTS & RULES", fontAsset, 26, FontStyles.Bold, new Color(1f, 0.85f, 0.2f));
                ptTitle.rectTransform.anchoredPosition = new Vector2(0f, 155f);

                string rulesString =
                    "<color=#FFD700><b>3x SEVEN (7)</b></color> : <b>100x Bet</b> (JACKPOT!)\n" +
                    "<i>* Seven also acts as a WILD symbol matching any payline!</i>\n\n" +
                    "<color=#FFA500><b>3x BELLS</b></color> : <b>50x Bet</b> + <color=#00FFFF><b>5 FREE SPINS (2x WIN MULTIPLIER!)</b></color>\n\n" +
                    "<color=#50C878><b>3x BARS</b></color> : <b>25x Bet</b>\n\n" +
                    "<color=#FF4500><b>3x CHERRIES</b></color> : <b>15x Bet</b>\n" +
                    "<color=#FF69B4><b>2x CHERRIES</b></color> : <b>5x Bet</b>   |   <b>1x CHERRY</b> : <b>2x Bet</b>";

                var ptRules = CreateTextMesh("RulesContent", paytableCardRect, rulesString, fontAsset, 15, FontStyles.Normal, Color.white);
                ptRules.rectTransform.sizeDelta = new Vector2(560, 240);
                ptRules.rectTransform.anchoredPosition = new Vector2(0f, 10f);
                ptRules.alignment = TextAlignmentOptions.Center;

                var ptCloseBtn = CreateSpriteButton("CloseBtn", paytableCardRect, closeBtnSprite, new Vector2(260f, 165f), new Vector2(40, 40));

                // Wire SlotUIView
                var uiSo = new SerializedObject(uiView);
                uiSo.FindProperty("balanceText").objectReferenceValue = cornerBalText;
                uiSo.FindProperty("cornerBalanceText").objectReferenceValue = cornerBalText;
                uiSo.FindProperty("statusBannerText").objectReferenceValue = statusText;
                uiSo.FindProperty("freeSpinsBanner").objectReferenceValue = fsBannerGo.gameObject;
                uiSo.FindProperty("freeSpinsText").objectReferenceValue = fsText;

                uiSo.FindProperty("quickBetPanel").objectReferenceValue = menuGo.gameObject;
                uiSo.FindProperty("quickBet10Btn").objectReferenceValue = btn10;
                uiSo.FindProperty("quickBet50Btn").objectReferenceValue = btn50;
                uiSo.FindProperty("quickBet100Btn").objectReferenceValue = btn100;
                uiSo.FindProperty("quickBetExitBtn").objectReferenceValue = btnExit;

                uiSo.FindProperty("paytableToggleButton").objectReferenceValue = rulesBtn;
                uiSo.FindProperty("soundToggleButton").objectReferenceValue = soundBtn;
                uiSo.FindProperty("soundToggleText").objectReferenceValue = soundBtn.GetComponentInChildren<TextMeshProUGUI>();
                uiSo.FindProperty("resetCreditsButton").objectReferenceValue = reloadBtn;

                uiSo.FindProperty("winPopupPanel").objectReferenceValue = winPopupGo.gameObject;
                uiSo.FindProperty("winPopupTitle").objectReferenceValue = winTitleText;
                uiSo.FindProperty("winPopupAmount").objectReferenceValue = winAmountText;
                uiSo.FindProperty("winPopupDesc").objectReferenceValue = winDescText;
                uiSo.FindProperty("winPopupCollectButton").objectReferenceValue = winCollectBtn;

                uiSo.FindProperty("paytablePanel").objectReferenceValue = paytablePopupGo.gameObject;
                uiSo.FindProperty("paytableCloseButton").objectReferenceValue = ptCloseBtn;
                uiSo.ApplyModifiedProperties();

                // Wire Presenter
                var presSo = new SerializedObject(presenter);
                var configAsset = AssetDatabase.LoadAssetAtPath<SlotGameConfigSO>($"{SettingsFolder}/SlotGameConfig.asset");
                var tableAsset = AssetDatabase.LoadAssetAtPath<PayoutTableSO>($"{SettingsFolder}/PayoutTable.asset");
                var sevenAsset = AssetDatabase.LoadAssetAtPath<SymbolDataSO>($"{SettingsFolder}/SymbolData_Seven.asset");
                var bellAsset = AssetDatabase.LoadAssetAtPath<SymbolDataSO>($"{SettingsFolder}/SymbolData_Bell.asset");
                var barAsset = AssetDatabase.LoadAssetAtPath<SymbolDataSO>($"{SettingsFolder}/SymbolData_Bar.asset");
                var cherryAsset = AssetDatabase.LoadAssetAtPath<SymbolDataSO>($"{SettingsFolder}/SymbolData_Cherry.asset");
                var symbolAssets = new List<SymbolDataSO> { sevenAsset, bellAsset, barAsset, cherryAsset };

                presSo.FindProperty("gameConfig").objectReferenceValue = configAsset;
                presSo.FindProperty("payoutTable").objectReferenceValue = tableAsset;

                var symProp = presSo.FindProperty("symbols");
                symProp.arraySize = symbolAssets.Count;
                for (int i = 0; i < symbolAssets.Count; i++)
                {
                    symProp.GetArrayElementAtIndex(i).objectReferenceValue = symbolAssets[i];
                }

                presSo.FindProperty("reelsManager").objectReferenceValue = reelsManager;
                presSo.FindProperty("machineView").objectReferenceValue = machineView;
                presSo.FindProperty("uiView").objectReferenceValue = uiView;
                presSo.FindProperty("audioService").objectReferenceValue = audioService;
                presSo.ApplyModifiedProperties();

                // Hook lever click zone to MachineView
                leverBtn.onClick.AddListener(() => {
                    presenter.SendMessage("HandleSpinRequest", UnityEngine.SendMessageOptions.DontRequireReceiver);
                });

                EditorUtility.SetDirty(presenter);
                EditorUtility.SetDirty(uiView);
                EditorUtility.SetDirty(machineView);
                EditorUtility.SetDirty(reelsManager);
                EditorUtility.SetDirty(audioService);

                // Mark scene dirty and save
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene, ScenePath);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                Debug.Log("<color=green>Slot Game Scene Setup Completed Successfully!</color>");
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
        }

        private static Sprite LoadSubSprite(string path, string subName)
        {
            var assets = AssetDatabase.LoadAllAssetsAtPath(path);
            foreach (var a in assets)
            {
                if (a is Sprite s && s.name == subName)
                {
                    return s;
                }
            }
            Debug.LogWarning($"Could not find sprite {subName} at {path}");
            return null;
        }

        private static SymbolDataSO CreateOrUpdateSymbolData(string assetName, SymbolType type, string displayName, Sprite sprite, Color color, int weight)
        {
            string path = $"{SettingsFolder}/{assetName}.asset";
            var data = AssetDatabase.LoadAssetAtPath<SymbolDataSO>(path);
            if (data == null)
            {
                data = ScriptableObject.CreateInstance<SymbolDataSO>();
                AssetDatabase.CreateAsset(data, path);
            }
            data.Initialize(type, displayName, sprite, color, weight);
            EditorUtility.SetDirty(data);
            return data;
        }

        private static PayoutTableSO CreateOrUpdatePayoutTable()
        {
            string path = $"{SettingsFolder}/PayoutTable.asset";
            var table = AssetDatabase.LoadAssetAtPath<PayoutTableSO>(path);
            if (table == null)
            {
                table = ScriptableObject.CreateInstance<PayoutTableSO>();
                AssetDatabase.CreateAsset(table, path);
            }
            table.InitializeDefaults();
            EditorUtility.SetDirty(table);
            return table;
        }

        private static SlotGameConfigSO CreateOrUpdateGameConfig()
        {
            string path = $"{SettingsFolder}/SlotGameConfig.asset";
            var config = AssetDatabase.LoadAssetAtPath<SlotGameConfigSO>(path);
            if (config == null)
            {
                config = ScriptableObject.CreateInstance<SlotGameConfigSO>();
                AssetDatabase.CreateAsset(config, path);
            }
            config.InitializeDefaults();
            EditorUtility.SetDirty(config);
            return config;
        }

        private static RectTransform CreateEmptyRect(string name, Transform parent, Vector2 size)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rect = go.AddComponent<RectTransform>();
            rect.sizeDelta = size;
            return rect;
        }

        private static Image CreateImageObject(string name, Transform parent, Sprite sprite)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var img = go.AddComponent<Image>();
            img.sprite = sprite;
            return img;
        }

        private static TextMeshProUGUI CreateTextMesh(string name, Transform parent, string text, TMP_FontAsset font, float size, FontStyles style, Color color)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var tmp = go.AddComponent<TextMeshProUGUI>();
            if (font != null) tmp.font = font;
            tmp.text = text;
            tmp.fontSize = size;
            tmp.fontStyle = style;
            tmp.color = color;
            tmp.alignment = TextAlignmentOptions.Center;
            return tmp;
        }

        private static Button CreateSpriteButton(string name, Transform parent, Sprite sprite, Vector2 pos, Vector2 size)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rect = go.AddComponent<RectTransform>();
            rect.anchoredPosition = pos;
            rect.sizeDelta = size;
            var img = go.AddComponent<Image>();
            img.sprite = sprite;
            img.preserveAspect = true;
            return go.AddComponent<Button>();
        }

        private static Button CreateRetroMenuButton(string name, Transform parent, string label, TMP_FontAsset font, Vector2 pos, Vector2 size, float fontSize = 17)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rect = go.AddComponent<RectTransform>();
            rect.anchoredPosition = pos;
            rect.sizeDelta = size;

            var img = go.AddComponent<Image>();
            img.color = new Color(0.12f, 0.16f, 0.24f, 0.65f);

            var btn = go.AddComponent<Button>();
            var colors = btn.colors;
            colors.normalColor = new Color(0.12f, 0.16f, 0.24f, 0.65f);
            colors.highlightedColor = new Color(0.18f, 0.38f, 0.45f, 0.95f);
            colors.pressedColor = new Color(0.1f, 0.25f, 0.3f, 1f);
            btn.colors = colors;

            var text = CreateTextMesh("Text", go.transform, label, font, fontSize, FontStyles.Bold, Color.white);
            var textRect = text.rectTransform;
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            return btn;
        }

        private static void EnsureDirectoryExists(string path)
        {
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
                AssetDatabase.Refresh();
            }
        }
    }
}
