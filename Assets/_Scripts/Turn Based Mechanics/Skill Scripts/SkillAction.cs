using System.Collections.Generic;

public class SkillAction {
    private SkillObject _data;
    private Actor _caster;
    private Actor _target;

    public SkillAction(SkillObject data) {
        _data = data;
    }
    
    public SkillAction(SkillObject data, Actor caster, Actor target) {
        _data = data;
        _caster = caster;
        _target = target;
    }

    public void SetTarget(Actor target) {
        _target = target;
    }

    public void SetSkill(SkillObject data) {
        _data = data;
    }

    public override string ToString() {
        return _data.GetSkillName();
    }

    public Actor Target() {
        return _target;
    }

    public SkillObject Data() {
        return _data;
    }

    public void ActivateSkill() {
        //_target.DepleteHitpoints(_data.damageAmount);
        //_target.RestoreHitpoints(_data.healAmount);
    }

    public void Clear() {
        _data = null;
        _target = null;
    }
}

public class StatIteration {

    private readonly ActorData baseData;

    public int Attack { get; private set; }
    public int Defense { get; private set; }
    public int StaminaRegen { get; private set; }

    public StatIteration(ActorData data) {
        baseData = data;
        Reset();
    }

    public void Reset() {
        Attack = baseData.BaseAttack();
        Defense = baseData.BaseDefense();
        StaminaRegen = baseData.StaminaRegenRate();
    }

    public void ComputeModifiers(List<EffectModifier> mods) {
        foreach (EffectModifier mod in mods) {
            Attack = (int) (mod.attackModifier * Attack);
            Defense = (int) (mod.defenseModifier * Defense);
            StaminaRegen = (int) (mod.staminaRegenModifier * StaminaRegen);
        }
    }
}