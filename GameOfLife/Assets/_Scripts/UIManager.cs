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

    [Header("Randomise Buttons")]
    [SerializeField] private Button _randomiseActiveButton;
    [SerializeField] private Button _randomiseAllSameButton;
    [SerializeField] private Button _randomiseAllDifferentButton;

    [Header("Clear Buttons")]
    [SerializeField] private Button _clearActiveButton;
    [SerializeField] private Button _clearAllButton;

    [Header("Step Buttons")]
    [SerializeField] private Button _stepActiveButton;
    [SerializeField] private Button _stepAllButton;

    [Header("Toggle Buttons")]
    [SerializeField] private Button _toggleActiveButton;
    [SerializeField] private Button _toggleAllButton;


    private int _activeSimulationId;

    private void Start()
    {
        SimulationManager.Instance.OnActiveSimulationChanged += OnActiveSimulationChanged;

        _addUniverseButton.onClick.AddListener(OnAddUniverseClicked);
        _deleteUniverseButton.onClick.AddListener(OnDeleteUniverseClicked);
        _updateUniverseButton.onClick.AddListener(OnUpdateUniverseClicked);

        _randomiseActiveButton.onClick.AddListener(OnRandomiseActiveClicked);
        _randomiseAllSameButton.onClick.AddListener(OnRandomiseAllSameClicked);
        _randomiseAllDifferentButton.onClick.AddListener(OnRandomiseAllDifferentClicked);

        _clearActiveButton.onClick.AddListener(OnClearActiveClicked);
        _clearAllButton.onClick.AddListener(OnClearAllClicked);

        _stepActiveButton.onClick.AddListener(OnStepActiveClicked);
        _stepAllButton.onClick.AddListener(OnStepAllClicked);

        _toggleActiveButton.onClick.AddListener(OnToggleActiveClicked);
        _toggleAllButton.onClick.AddListener(OnToggleAllClicked);

        foreach (Toggle t in _birthToggles)
            t.onValueChanged.AddListener(UpdateRulesText);
        foreach (Toggle t in _survivalToggles)
            t.onValueChanged.AddListener(UpdateRulesText);
    }

    private void OnAddUniverseClicked()
    {
        if (SimulationManager.Instance.GetSimulationFromName(_simulationNameInput.text) != null)
        {
            Debug.LogError("A universe with this name already exists");
            return;
        }

        SimulationManager.Instance.AddSimulation(_simulationNameInput.text, GenerateRuleString());
    }

    private void OnDeleteUniverseClicked()
    {
        SimulationManager.Instance.RemoveSimulation(_activeSimulationId);
    }

    private void OnUpdateUniverseClicked()
    {
        if (SimulationManager.Instance.GetSimulationFromName(_simulationNameInput.text, _activeSimulationId) != null)
        {
            Debug.LogError("A universe with this name already exists");
            return;
        }

        SimulationManager.Instance.UpdateSimulation(_simulationNameInput.text, GenerateRuleString());
    }

    private void OnRandomiseActiveClicked()
    {
        SimulationManager.Instance.RandomiseActive();
    }

    private void OnRandomiseAllSameClicked()
    {
        SimulationManager.Instance.RandomiseAllSame();
    }

    private void OnRandomiseAllDifferentClicked()
    {
        SimulationManager.Instance.RandomiseAllDifferent();
    }

    private void OnClearActiveClicked()
    {
        SimulationManager.Instance.ClearActive();
    }

    private void OnClearAllClicked()
    {
        SimulationManager.Instance.ClearAll();
    }

    private void OnStepActiveClicked()
    {
        SimulationManager.Instance.StepActive();
    }

    private void OnStepAllClicked()
    {
        SimulationManager.Instance.StepAll();
    }

    private void OnToggleActiveClicked()
    {
        SimulationManager.Instance.ToggleActiveRunning();
    }

    private void OnToggleAllClicked()
    {
        SimulationManager.Instance.ToggleAllRunning();
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