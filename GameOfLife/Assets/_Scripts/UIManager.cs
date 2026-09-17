using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Linq;

public class UIManager : MonoBehaviour
{
    [Header("Universe Details UI")]
    [SerializeField] private TMP_InputField _simulationNameInput;
    [SerializeField] private TMP_Text _rulesText;
    [SerializeField] private Toggle[] _birthToggles;
    [SerializeField] private Toggle[] _survivalToggles;
    [SerializeField] private Button _addUniverseButton;
    [SerializeField] private Button _deleteUniverseButton;
    [SerializeField] private Button _updateUniverseButton;

    private int _activeSimulationId;

    private void Start()
    {
        SimulationManager.Instance.OnActiveSimulationChanged += OnActiveSimulationChanged;

        _addUniverseButton.onClick.AddListener(OnAddUniverseButtonClicked);
        _deleteUniverseButton.onClick.AddListener(OnDeleteUniverseButtonClicked);
        _updateUniverseButton.onClick.AddListener(OnUpdateUniverseButtonClicked);

        foreach (Toggle t in _birthToggles)
            t.onValueChanged.AddListener(UpdateRulesText);
        foreach (Toggle t in _survivalToggles)
            t.onValueChanged.AddListener(UpdateRulesText);
    }

    private void OnAddUniverseButtonClicked()
    {
        if (SimulationManager.Instance.GetSimulationFromName(_simulationNameInput.text) != null)
        {
            Debug.LogError("A universe with this name already exists");
            return;
        }

        SimulationManager.Instance.AddSimulation(_simulationNameInput.text, GenerateRuleString());
    }

    private void OnDeleteUniverseButtonClicked()
    {
        SimulationManager.Instance.RemoveSimulation(_activeSimulationId);
    }

    private void OnUpdateUniverseButtonClicked()
    {

    }

    private void OnActiveSimulationChanged(int index)
    {
        SimulationInstance sim = SimulationManager.Instance.GetSimulationFromId(index);

        _activeSimulationId = sim.Id;
        _simulationNameInput.text = sim.Name;

        ApplyRuleStringToCheckboxes(sim.RuleString);
    }

    private void ApplyRuleStringToCheckboxes(string ruleString)
    {
        // Reset all checkboxes first
        foreach (var toggle in _birthToggles) toggle.isOn = false;
        foreach (var toggle in _survivalToggles) toggle.isOn = false;

        // Parse "B3/S23" format
        string[] parts = ruleString.Split('/');

        foreach (string part in parts)
        {
            if (part.Length < 2) continue;

            char type = part[0];
            var toggles = type == 'B' ? _birthToggles :
                          type == 'S' ? _survivalToggles : null;

            if (toggles == null) continue;

            // Check each digit in the rule string
            for (int i = 1; i < part.Length; i++)
            {
                if (char.IsDigit(part[i]))
                {
                    int index = part[i] - '0';
                    if (index >= 0 && index < toggles.Length)
                    {
                        toggles[index].isOn = true;
                    }
                }
            }
        }
    }

    private string GenerateRuleString()
    {
        string B = "";
        string S = "";

        for (int i = 0; i < 9; i++)
        {
            if (_birthToggles[i].isOn) B += i;
            if (_survivalToggles[i].isOn) S += i;
        }

        return $"B{B}/S{S}";
    }

    private void UpdateRulesText(bool on)
    {
        _rulesText.text = "Rules: " + GenerateRuleString();
    }
}