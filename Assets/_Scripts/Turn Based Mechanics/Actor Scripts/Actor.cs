using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Actor : MonoBehaviour, IComparable<Actor>
{
    #region Data Attributes
    [SerializeField] public readonly ActorData data;
    public StatIteration ActiveData { get; private set; }
    [SerializeField] private string uniqueID;
    #endregion Data Attributes
    
    #region Accessors
    public ActorData Data() { return data; }

    public string UniqueID() { return uniqueID; }
    #endregion Accessors
    
    #region Variable Attributes

    [SerializeField] private int _hitpoints;
    private bool _defeated;
    private int _stamina;

    #endregion Variable Attributes

    #region Level Skills, Bonbons & Modifiers

    protected List<SkillObject> skillList;

    protected List<BonbonObject> bonbonList;

    protected List<Effect> effectList;

    #endregion

    #region Bonbon Inventory

    protected BonbonObject[] bonbonInventory = new BonbonObject[4];

    #endregion

    protected virtual void Start()
    {
        InitializeAttributes();
        InitializeLevelObjects();
    }

    protected virtual void InitializeAttributes() {
        ActiveData = new StatIteration(data);
        _hitpoints = data.MaxHitpoints();
        _stamina = data.MaxStamina();
        _defeated = false;
    }

    protected virtual void InitializeLevelObjects() { }

    public void TurnStart() {
        List<int> spentEffects = new List<int>();
        for (int i = 0; i < effectList.Count; i++) {
            effectList[i].PerformActions(ActiveData, this);
            if (effectList[i].IsSpent()) spentEffects.Add(i);
        } RemoveEffects(spentEffects);
    }

    private void ComputeStats() {
        List<EffectModifier> modifiers = new List<EffectModifier>();
        foreach (BonbonObject bonbon in bonbonInventory) {
            modifiers.AddRange(bonbon.effects);
        } foreach (Effect effect in effectList) {
            modifiers.Add(effect.modifiers);
        } ActiveData.ComputeModifiers(modifiers);
    }

    public void ApplyEffects(List<Effect> effects) {
        effectList.AddRange(effects);
        ComputeStats();
    }

    public void RemoveEffects(List<int> effectIndices) {
        foreach (int effectIndex in effectIndices) effectList.RemoveAt(effectIndex);
        ComputeStats();
    }

    //Returns true if Actor has no remaining health.
    public bool DepleteHitpoints(int damage) {
        damage *= ActiveData.Defense / 100;

        if (_hitpoints - damage <= 0) {
            _hitpoints = 0;
            _defeated = true;
            Debug.Log($"{this.data.DisplayName()} has fallen!");
            return true;
        }
        _hitpoints -= damage;
        return false;
    }
    
    //Returns true if over maximum hitpoints.
    //Does not heal if Actor is defeated.
    public bool RestoreHitpoints(int heal) {
        if (_hitpoints + heal > data.MaxHitpoints()) {
            _hitpoints = data.MaxHitpoints();
            return true;
        }
        if (!_defeated) {
            _hitpoints += heal;
        }
        return false;
    }

    public void InsertBonbon(int slot, BonbonObject bonbon) {
        if (bonbonInventory[slot] == null) {
            bonbonInventory[slot] = bonbon;
        } else Debug.LogError("Inventory slot was not available;");
    }

    public float Hitpoints() {
        return _hitpoints;
    }

    public bool Defeated() {
        return _defeated;
    }
    
    public bool HasRemainingStamina() {
        return _stamina > 0;
    }

    public bool HasRemainingStamina(int cost) {
        return _stamina - cost > 0;
    }
    
    public void RefundStamina(int cost) {
        if (_stamina + cost > data.MaxStamina()) {
            _stamina = data.MaxStamina();
        } else {
            _stamina += cost;
        }
    }
    
    #region Comparators
    public int CompareTo(Actor actor) {
        return data.BaseSpeed() - actor.data.BaseSpeed();
    }
    
    public override bool Equals(object obj) {
        var item = obj as Actor;

        if (item == null) {
            return false;
        }

        return item.Data().ID() == data.ID();
    }

    public override int GetHashCode() {
        return HashCode.Combine(base.GetHashCode(), name, gameObject, uniqueID);
    }

    #endregion Comparators
}