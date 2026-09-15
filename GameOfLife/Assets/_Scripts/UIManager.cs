using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UIManager : MonoBehaviour
{
    [Header("Control Buttons")]
    public Button playPauseBtn;
    public Button stepBtn;
    public Button randomBtn;
    public Button clearBtn;

    [Header("Simulation Management")]
    public Button addSimBtn;
    public Button removeSimBtn;
    public TMP_InputField nameInput;
    public TMP_InputField ruleInput;

    private SimulationManager _manager;

    void Start()
    {
        _manager = SimulationManager.Instance;

        // Wire up Buttons
        playPauseBtn.onClick.AddListener(() =>
        {
            _manager.ToggleRunning();
            UpdatePlayPauseText();
        });

        stepBtn.onClick.AddListener(_manager.StepOnce);
        randomBtn.onClick.AddListener(_manager.RandomiseActive);
        clearBtn.onClick.AddListener(_manager.ClearActive);

        addSimBtn.onClick.AddListener(AddNewSimulation);
        removeSimBtn.onClick.AddListener(RemoveCurrentSimulation);

        ruleInput.onEndEdit.AddListener(_manager.UpdateRuleForActive);

        UpdatePlayPauseText();
    }

    private void AddNewSimulation()
    {
        string name = string.IsNullOrEmpty(nameInput.text) ? "New Universe" : nameInput.text;
        string rule = string.IsNullOrEmpty(ruleInput.text) ? "B3/S23" : ruleInput.text;
        _manager.AddSimulation(name, rule);
    }

    private void RemoveCurrentSimulation()
    {
        _manager.RemoveSimulation(_manager.ActiveIndex);
    }

    private void UpdatePlayPauseText()
    {
        // Note: You might want to check if running via a getter in Manager
        // For now, we assume it's paused if we don't know. 
        // Ideally, add a public bool IsRunning to SimulationManager.
    }
}