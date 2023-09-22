using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UIElements;
using Slider = UnityEngine.UI.Slider;

public class Healthbar : MonoBehaviour {
    private Slider slider;
    [SerializeField] private BattleStateMachine _stateMachine;
    [SerializeField] private Actor actor;
    void Start() {
        slider = GetComponent<Slider>();
        _stateMachine.OnStateTransition += UpdateHealthBar;
    }

    private void UpdateHealthBar(BattleStateMachine.BattleState state, BattleStateInput input) {
        if (state is not BattleStateMachine.AnimateState) return;
        float currHealth = actor.Hitpoints();
        float maxHealth = actor.data.MaxHitpoints();

        slider.value = currHealth / maxHealth;
    }
}
