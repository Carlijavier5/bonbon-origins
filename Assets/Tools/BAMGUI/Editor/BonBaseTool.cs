﻿//using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using CJUtils;
using PseudoDataStructures;

/// <summary>
/// Because the structure I devised for the MADGUI has been quite comfortable to work with, I'm building 
/// this tool using a similar structure. This tool has different needs and less complexity than the MAD,
/// thus I'm creating a new tool altogether rather than inheriting from the former's abstract base;
/// </summary>
namespace BonbonAssetManager {
    public abstract class BonBaseTool : ScriptableObject {

        protected BAMGUI MainGUI;

        public string SelectedPath { get; protected set; }

        public static T CreateTool<T>(BAMGUI mainGUI) where T : BonBaseTool {
            T tool = CreateInstance<T>();
            tool.MainGUI = mainGUI;
            return tool;
        }

        public abstract void Initialize();

        public virtual void ShowGUI() { }
    }

    public class BonbonManager : BonBaseTool {

        private BaseHierarchy<BonbonBlueprint> bonbonHierarchy;
        private BonbonBlueprint selectedBonbon;
        private List<BonbonBlueprint> bonbonList;
        private AssetCreator<BonbonBlueprint> assetCreator;

        public override void Initialize() {
            assetCreator = new AssetCreator<BonbonBlueprint>(MainGUI.assetPaths[(int) BAMGUI.ToolType.BonbonManager]);
            bonbonHierarchy = new BaseHierarchy<BonbonBlueprint>(this);
            bonbonHierarchy.OnPathSelection += BonbonManager_OnPathSelection;
            assetCreator.OnAssetCreation += bonbonHierarchy.ReloadHierarchy;
            UpdateBonbonList();
        }

        private int buttonSize = 50;

        private Vector2 bonbonScroll;
        private Vector2 recipeScroll;
        private Vector2 previewScroll;

        public void SetSelectedBonbon(BonbonBlueprint bonbon) {
            selectedBonbon = bonbon;
            UpdateBonbonList();
        }

        private void UpdateBonbonList() {
            bonbonList = new List<BonbonBlueprint>(MainGUI.GlobalBonbonList);
            List<BonbonBlueprint> invalidBonbons = new List<BonbonBlueprint>();
            foreach (BonbonBlueprint bonbon in bonbonList) {
                bonbon.UpdateRecipeSize();

                if (bonbon == selectedBonbon) {
                    invalidBonbons.Add(bonbon);
                    continue;
                }

                foreach (BonbonBlueprint ingredient in bonbon.recipe) {
                    if (ingredient == selectedBonbon) {
                        invalidBonbons.Add(bonbon);
                        break;
                    }
                }
            } foreach (BonbonBlueprint bonbon in invalidBonbons) bonbonList.Remove(bonbon);
        }

        public override void ShowGUI() {
            using (new EditorGUILayout.HorizontalScope()) {
                using (new EditorGUILayout.VerticalScope()) {
                    bonbonHierarchy.ShowGUI();
                    assetCreator.ShowCreator();
                }
                    
                using (new EditorGUILayout.VerticalScope()) {
                    MainGUI.DrawToolbar();

                    if (selectedBonbon != null) {
                        //DrawBonbonProperties();
                        DrawBonbonGroup();
                        DrawRecipeDropSlots();
                        DrawRecipePreview(selectedBonbon);
                    } else {
                        EditorUtils.DrawScopeCenteredText("Select a Bonbon to edit it here;");
                    }
                }
            }
        }

        private void BonbonManager_OnPathSelection(string path) {
            SelectedPath = path;
            SetSelectedBonbon(AssetDatabase.LoadAssetAtPath<BonbonBlueprint>(path));
        }

        private void DrawBonbonGroup() {
            EditorUtils.WindowBoxLabel("Available Bonbons");

            using (var vscope = new EditorGUILayout.VerticalScope(GUILayout.ExpandWidth(true))) {
                using (var scope = new EditorGUILayout.ScrollViewScope(bonbonScroll, GUILayout.Height(buttonSize * 2.5f))) {
                    bonbonScroll = scope.scrollPosition;
                    DrawWrappedSequence(500);
                }
            }
        }

        private void DrawWrappedSequence(int maxWidth) {

            int perRow = maxWidth / buttonSize;
            int rows = bonbonList.Count / perRow;
            for (int i = 0; i < rows + 1; i++) {
                using (new EditorGUILayout.HorizontalScope()) {
                    for (int j = i * perRow; j < Mathf.Min((i + 1) * perRow, bonbonList.Count); j++) {
                        BAMUtils.DrawAssetDragButton(bonbonList[j], new GUIContent(bonbonList[j].texture), buttonSize);
                    }
                }
            }
        }

        private void DrawRecipeDropSlots() {
            EditorUtils.WindowBoxLabel("Recipe");
            GUI.enabled = false;
            using (var scope = new EditorGUILayout.ScrollViewScope(recipeScroll, EditorStyles.textField, 
                                                                   GUILayout.ExpandHeight(false), GUILayout.Height(buttonSize * 2.5f))) {
                GUI.enabled = true;
                recipeScroll = scope.scrollPosition;

                using (new EditorGUILayout.HorizontalScope()) {
                    BonbonBlueprint acceptedBonbon = BAMUtils.DrawDragAcceptButton<BonbonBlueprint>(FieldUtils.DnDFieldType.Add,
                                                                                                    MainGUI.assetRefs.dndFieldAssets,
                                                                                                    GUILayout.Width(buttonSize * 2),
                                                                                                    GUILayout.Height(buttonSize * 2));
                    if (acceptedBonbon != null) UpdateBonbonRecipe(selectedBonbon, acceptedBonbon);

                    using (new EditorGUILayout.VerticalScope()) {
                        using (new EditorGUILayout.HorizontalScope()) {
                            foreach (BonbonBlueprint bonbon in selectedBonbon.recipe) {
                                if (bonbon != null) BAMUtils.DrawAssetDragButton(bonbon, new GUIContent(bonbon.texture), buttonSize);
                                else DrawEmptyBox();
                            }
                        } BonbonBlueprint removeObject = BAMUtils.DrawDragAcceptButton<BonbonBlueprint>(FieldUtils.DnDFieldType.Remove,
                                                                                                        MainGUI.assetRefs.dndFieldAssets,
                                                                                                        GUILayout.Width(buttonSize * 4.4f),
                                                                                                        GUILayout.Height(buttonSize * 0.8f));
                        if (removeObject != null) RemoveFromRecipe(selectedBonbon, removeObject);
                    }
                } 
            }
        }

        private void UpdateBonbonRecipe(BonbonBlueprint recipeBonbon, BonbonBlueprint ingredientBonbon) {
            recipeBonbon.AddRecipeSlot(ingredientBonbon);
            EditorUtility.SetDirty(recipeBonbon);
        }

        private void RemoveFromRecipe(BonbonBlueprint recipeBonbon, BonbonBlueprint ingredientBonbon) {
            recipeBonbon.RemoveRecipeSlot(ingredientBonbon);
            EditorUtility.SetDirty(recipeBonbon);
        }

        private void DrawEmptyBox() {
            using (new EditorGUILayout.VerticalScope(UIStyles.WindowBox)) {
                GUILayout.Box("", GUILayout.Width(buttonSize), GUILayout.Height(buttonSize));
            }
        }

        private void DrawRecipePreview(BonbonBlueprint bonbonObject) {
            EditorUtils.WindowBoxLabel("Recipe Preview");
            using (var scope = new EditorGUILayout.ScrollViewScope(previewScroll)) {
                previewScroll = scope.scrollPosition;
                DrawPreviewLevel(bonbonObject);
            }
        }

        private void DrawPreviewLevel(BonbonBlueprint bonbonObject) {
            if (bonbonObject == null) return;
            using (new EditorGUILayout.VerticalScope(UIStyles.WindowBox)) {
                var levelRect = DrawPaddedButton(bonbonObject);
                if (bonbonObject.recipe == null) bonbonObject.UpdateRecipeSize();
                /*float levelStart = 0;
                float levelWidth = 0;
                int buttonCount = 0;*/
                
                using (new EditorGUILayout.HorizontalScope()) {
                    foreach (BonbonBlueprint ingredient in bonbonObject.recipe) {
                        if (ingredient != null) DrawPreviewLevel(ingredient);
                        //if (buttonCount == 0) levelStart = (rect.xMax - rect.x) / 2;
                        //levelWidth = (rect.xMax - rect.x) / 2 - levelStart;
                    }
                } /*var lineHeight = levelRect.y - 2;
                Handles.DrawLine(new Vector2(levelStart, lineHeight), new Vector2(levelStart + levelWidth, lineHeight));*/
            }
        }

        private Rect DrawPaddedButton(BonbonBlueprint ingredient) {
            using (new EditorGUILayout.HorizontalScope(UIStyles.WindowBox)) {
                GUILayout.FlexibleSpace();
                using (new EditorGUILayout.VerticalScope(UIStyles.WindowBox, GUILayout.Width(buttonSize), GUILayout.Height(buttonSize), GUILayout.ExpandWidth(false))) {
                    GUILayout.FlexibleSpace();
                    GUILayout.Label(ingredient.texture, UIStyles.CenteredLabel, GUILayout.Width(buttonSize), GUILayout.Height(buttonSize));
                    GUILayout.FlexibleSpace();
                } GUILayout.FlexibleSpace();
                return new Rect();
            }
        }
    }

    public class SkillManager : BonBaseTool {

        private BaseHierarchy<SkillObject> skillHierarchy;
        private SkillObject selectedSkill;
        private Editor skillInspector;
        private AssetCreator<SkillObject> assetCreator;

        private System.Type[] actionTypes;
        private ImmediateAction[] foundActions;

        public override void Initialize() {
            assetCreator = new AssetCreator<SkillObject>(MainGUI.assetPaths[(int) BAMGUI.ToolType.SkillManager]);
            skillHierarchy = new BaseHierarchy<SkillObject>(this);
            skillHierarchy.OnPathSelection += SkillHierarchy_OnPathSelection;
            assetCreator.OnAssetCreation += skillHierarchy.ReloadHierarchy;
            actionTypes = ActionUtils.FetchAssemblyChildren(new System.Type[] { typeof(ImmediateAction.Generic),
                                                                                typeof(ImmediateAction.SkillOnly)});
        }

        void OnDisable() {
            DestroyImmediate(skillInspector);
        }

        public void SkillHierarchy_OnPathSelection(string path) {
            SelectedPath = path;
            DestroyImmediate(skillInspector);
            skillInspector = null;
            SetSelectedSkill(AssetDatabase.LoadAssetAtPath<SkillObject>(path));
        }

        private void SetSelectedSkill(SkillObject skill) {
            selectedSkill = skill;
            if (selectedSkill.immediateActions == null) selectedSkill.immediateActions = new List<ImmediateAction>();
            foundActions = ActionUtils.FetchAvailableActions(selectedSkill.immediateActions, actionTypes);
        }

        public override void ShowGUI() {
            using (new EditorGUILayout.HorizontalScope()) {
                using (new EditorGUILayout.VerticalScope()) {
                    skillHierarchy.ShowGUI();
                    assetCreator.ShowCreator();
                }
                    
                using (new EditorGUILayout.VerticalScope()) {
                    MainGUI.DrawToolbar();

                    if (selectedSkill != null) {
                        if (skillInspector is null) skillInspector = Editor.CreateEditor(selectedSkill);
                        if (selectedSkill != null) skillInspector.OnInspectorGUI();
                        else Debug.Log("Nope");

                        EditorGUILayout.Separator();
                        EditorUtils.DrawSeparatorLines(" Immediate Actions");
                        EditorGUILayout.Separator();

                        if (ActionUtils.DrawAvailableActions(ref selectedSkill.immediateActions,
                                                             ref foundActions, actionTypes)) EditorUtility.SetDirty(selectedSkill);
                    } else {
                        EditorUtils.DrawScopeCenteredText("Select a Skill to edit it here;");
                    }
                }
            }
        }
    }

    public class EffectManager : BonBaseTool {

        private BaseHierarchy<EffectBlueprint> effectHierarchy;
        private EffectBlueprint selectedEffect;
        private Editor effectInspector;
        private AssetCreator<EffectBlueprint> assetCreator;

        private System.Type[] actionTypes;
        private ImmediateAction[] foundActions;

        public override void Initialize() {
            assetCreator = new AssetCreator<EffectBlueprint>(MainGUI.assetPaths[(int) BAMGUI.ToolType.EffectManager]);
            effectHierarchy = new BaseHierarchy<EffectBlueprint>(this);
            effectHierarchy.OnPathSelection += EffectHierarchy_OnPathSelection;
            assetCreator.OnAssetCreation += effectHierarchy.ReloadHierarchy;
            actionTypes = ActionUtils.FetchAssemblyChildren(new System.Type[] { typeof(ImmediateAction.Generic),
                                                                                typeof(ImmediateAction.EffectOnly)});
        }

        void OnDisable() {
            DestroyImmediate(effectInspector);
        }

        public void EffectHierarchy_OnPathSelection(string path) {
            SelectedPath = path;
            DestroyImmediate(effectInspector);
            effectInspector = null;
            SetSelectedSkill(AssetDatabase.LoadAssetAtPath<EffectBlueprint>(path));
        }

        private void SetSelectedSkill(EffectBlueprint effect) {
            selectedEffect = effect;
            if (selectedEffect.actions == null) selectedEffect.actions = new List<ImmediateAction>();
            foundActions = ActionUtils.FetchAvailableActions(selectedEffect.actions, actionTypes);
        }

        public override void ShowGUI() {
            using (new EditorGUILayout.HorizontalScope()) {
                using (new EditorGUILayout.VerticalScope()) {
                    effectHierarchy.ShowGUI();
                    assetCreator.ShowCreator();
                }
                    
                using (new EditorGUILayout.VerticalScope()) {
                    MainGUI.DrawToolbar();

                    if (selectedEffect != null) {
                        if (effectInspector is null) effectInspector = Editor.CreateEditor(selectedEffect);
                        if (selectedEffect != null) effectInspector.OnInspectorGUI();
                        else Debug.Log("Nope");

                        EditorGUILayout.Separator();
                        EditorUtils.DrawSeparatorLines(" Immediate Actions", true);
                        EditorGUILayout.Separator();

                        if (ActionUtils.DrawAvailableActions(ref selectedEffect.actions,
                                                             ref foundActions, actionTypes)) EditorUtility.SetDirty(selectedEffect);
                    } else {
                        EditorUtils.DrawScopeCenteredText("Select an Effect to edit it here;");
                    }
                }
            }
        }
    }

    public class ActorManager : BonBaseTool {

        private BaseHierarchy<ActorData> actorHierarchy;
        private ActorData selectedActor;
        private AssetCreator<ActorData> assetCreator;

        private List<BonbonBlueprint> bonbonList => MainGUI.GlobalBonbonList;
        private List<SkillObject> skillList => MainGUI.GlobalSkillList;

        private List<SkillObject>[] skillMap;
        private List<BonbonBlueprint>[] bonbonMap;

        public override void Initialize() {
            assetCreator = new AssetCreator<ActorData>(MainGUI.assetPaths[(int) BAMGUI.ToolType.ActorManager]);
            actorHierarchy = new BaseHierarchy<ActorData>(this);
            actorHierarchy.OnPathSelection += ActorManager_OnPathSelection;
            assetCreator.OnAssetCreation += actorHierarchy.ReloadHierarchy;
        }

        private void ActorManager_OnPathSelection(string path) {
            SelectedPath = path;
            SetSelectedActor(AssetDatabase.LoadAssetAtPath<ActorData>(path));
        }

        private void SetSelectedActor(ActorData data) {
            selectedActor = data;
            InitializeActorMaps();
        }

        private void InitializeActorMaps() {
            if (selectedActor.skillMap != null) {
                skillMap = selectedActor.skillMap.ToListArray();
                selectedActor.skillMap = new ArrayArray<SkillObject>(VerifyMapSize(skillMap));
            } if (selectedActor.bonbonMap != null) {
                bonbonMap = selectedActor.bonbonMap.ToListArray();
                selectedActor.bonbonMap = new ArrayArray<BonbonBlueprint>(VerifyMapSize(bonbonMap));
            }
        }

        private List<T>[] VerifyMapSize<T>(List<T>[] listArr) {
            List<T>[] outputArr = new List<T>[8];
            if (listArr.Length < 8) System.Array.Copy(listArr, outputArr, listArr.Length);
            else outputArr = listArr;
            return outputArr;
        }

        public override void ShowGUI() {
            using (new EditorGUILayout.HorizontalScope()) {
                using (new EditorGUILayout.VerticalScope()) {
                    actorHierarchy.ShowGUI();
                    assetCreator.ShowCreator();
                }
                
                using (new EditorGUILayout.VerticalScope()) {
                    MainGUI.DrawToolbar();

                    if (selectedActor != null) {
                        DrawActorStats();
                        using (new EditorGUILayout.HorizontalScope()) {
                            DrawMap(skillMap);
                        }
                    } else {
                        EditorUtils.DrawScopeCenteredText("Select an Actor to edit it here;");
                    }
                }
            }
        }

        private void DrawActorStats() {
            CJToolAssets.StatFieldAssets statAssets = MainGUI.assetRefs.statFieldAssets;
            using (new EditorGUILayout.VerticalScope(UIStyles.WindowBox)) {
                using (var scope = new EditorGUI.ChangeCheckScope()) {
                    EditorUtils.WindowBoxLabel("Actor Notation");
                    using (new EditorGUILayout.VerticalScope(UIStyles.WindowBox)) {
                        selectedActor.SetDisplayName(EditorGUILayout.TextField("Actor Name", selectedActor.DisplayName));
                        selectedActor.SetID(EditorGUILayout.TextField("Actor ID", selectedActor.ID));
                    } EditorUtils.WindowBoxLabel("Actor Stats");
                    using (new EditorGUILayout.HorizontalScope(UIStyles.WindowBox)) {
                        using (new EditorGUILayout.VerticalScope()) {
                            selectedActor.SetMaxHitpoints(EditorGUILayout.IntField(new GUIContent(" Max Hitpoints", statAssets.hitpoints),
                                                                selectedActor.MaxHitpoints));
                            selectedActor.SetMaxStamina(EditorGUILayout.IntField(new GUIContent(" Max Stamina", statAssets.stamina),
                                                                selectedActor.MaxStamina));
                            selectedActor.SetStaminaRegenRate(EditorGUILayout.IntField(new GUIContent(" Stamina Regen", statAssets.staminaRegen),
                                                                selectedActor.StaminaRegenRate));
                        } EditorGUILayout.Separator();
                        using (new EditorGUILayout.VerticalScope()) {
                            selectedActor.SetBasePotency(EditorGUILayout.IntField(new GUIContent(" Base Potency", statAssets.attack),
                                                                selectedActor.BasePotency));
                            selectedActor.SetBaseDefense(EditorGUILayout.IntField(new GUIContent(" Base Defense", statAssets.defense),
                                                                selectedActor.BaseDefense));
                            selectedActor.SetBaseSpeed(EditorGUILayout.IntField(new GUIContent(" Base Speed", statAssets.speed),
                                                                selectedActor.BaseSpeed));
                        }
                    } if (scope.changed) EditorUtility.SetDirty(selectedActor);
                }
            }
        }

        private void DrawMap<T>(List<T>[] listArr) where T : ScriptableObject {
            using (new EditorGUILayout.VerticalScope(UIStyles.WindowBox)) {
                EditorUtils.WindowBoxLabel("Actor Skill Map");
                for (int i = 0; i < listArr.Length; i++) {
                    using (new EditorGUILayout.HorizontalScope(UIStyles.WindowBox)) {
                        EditorUtils.WindowBoxLabel($"Level {i + 1}", GUILayout.Width(48), GUILayout.Height(45));
                        DrawLevelDropdown<T>(listArr[i], 35);
                    }
                } 
            }
        }

        private void DrawLevelDropdown<T>(List<T> list, float buttonSize) where T : ScriptableObject {
            T acceptedElement = BAMUtils.DrawDragAcceptButton<T>(FieldUtils.DnDFieldType.Add,
                                                                 MainGUI.assetRefs.dndFieldAssets, GUILayout.Width(buttonSize),
                                                                 GUILayout.Height(buttonSize));
            if (acceptedElement != null && !list.Contains(acceptedElement)) list.Add(acceptedElement);
            using (var scope = new EditorGUILayout.ScrollViewScope(Vector2.zero, false, false, GUI.skin.horizontalScrollbar, GUIStyle.none, GUI.skin.scrollView,
                                                                   GUILayout.ExpandWidth(true), GUILayout.Height(buttonSize * 1.6f))) {
                //lowerScroll = scope.scrollPosition;
                for (int i = 0; i < list.Count; i++) {
                    using (new EditorGUILayout.HorizontalScope(UIStyles.WindowBox)) {
                        BAMUtils.DrawAssetDragButton(list[i], new GUIContent(list[i].name),
                                                     new Vector2(EditorUtils.MeasureTextWidth(list[i].name, GUI.skin.font) + 15, buttonSize));
                    }
                }
            }
            T removedElement = BAMUtils.DrawDragAcceptButton<T>(FieldUtils.DnDFieldType.Remove,
                                                                MainGUI.assetRefs.dndFieldAssets, GUILayout.Width(buttonSize),
                                                                GUILayout.Height(buttonSize));
            if (removedElement != null) list.Remove(removedElement);
        }
    }
}