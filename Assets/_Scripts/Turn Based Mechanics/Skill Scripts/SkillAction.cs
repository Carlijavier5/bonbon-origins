using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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

    public override String ToString() {
        return _data.GetSkillName();
    }

    public Actor Target() {
        return _target;
    }

    public SkillObject Data() {
        return _data;
    }

    public void ActivateSkill() {
        _target.DepleteHitpoints(_data.damageAmount);
        _target.RestoreHitpoints(_data.healAmount);
    }

    public void Clear() {
        _data = null;
        _target = null;
    }
}
