using System;
using Timberborn.AutomationBuildings;
using Timberborn.AutomationBuildingsUI;
using Timberborn.BaseComponentSystem;
using Timberborn.CoreUI;
using Timberborn.DropdownSystem;
using Timberborn.EntityPanelSystem;
using Timberborn.Localization;
using UnityEngine.UIElements;

namespace Calloatti.StorageMonitor
{
  internal class StorageMonitorFragment : IEntityPanelFragment
  {
    private static readonly string ModeLocKeyPrefix = "Building.StorageMonitor.Mode.";

    private static readonly string TurnOnIfLocKey = "Building.StorageMonitor.TurnOnIf";
    private static readonly string TurnOffIfLocKey = "Building.StorageMonitor.TurnOffIf";
    private static readonly string MeasurementQuantityLocKey = "Building.StorageMonitor.MeasurementQuantity";
    private static readonly string MeasurementPercentLocKey = "Building.StorageMonitor.MeasurementPercent";

    private readonly VisualElementLoader _visualElementLoader;
    private readonly DropdownItemsSetter _dropdownItemsSetter;
    private readonly RadioToggleFactory _radioToggleFactory;
    private readonly ILoc _loc;

    private StorageMonitor _storageMonitor;
    private StorageMonitorGoodsDropdownProvider _goodsDropdownProvider;

    private VisualElement _root;
    private Dropdown _goodDropdown;
    private RadioToggle _modeRadioToggle;
    private Toggle _includeInputsToggle;
    private Label _measurement;

    private IntegerField _thresholdOnField;
    private Label _fillRateLabelOn;
    private PreciseSlider _fillRateSliderOn;

    private IntegerField _thresholdOffField;
    private Label _fillRateLabelOff;
    private PreciseSlider _fillRateSliderOff;

    public StorageMonitorFragment(
        VisualElementLoader visualElementLoader,
        DropdownItemsSetter dropdownItemsSetter,
        RadioToggleFactory radioToggleFactory,
        ILoc loc)
    {
      _visualElementLoader = visualElementLoader;
      _dropdownItemsSetter = dropdownItemsSetter;
      _radioToggleFactory = radioToggleFactory;
      _loc = loc;
    }

    public VisualElement InitializeFragment()
    {
      _root = _visualElementLoader.LoadVisualElement("Game/EntityPanel/ResourceCounterFragment");
      var bottomSection = _root.Q<VisualElement>("BottomSection");

      _modeRadioToggle = _radioToggleFactory.CreateLocalizable<ResourceCounterMode>(ModeLocKeyPrefix, _root.Q<VisualElement>("ModeRadioToggleContainer"));
      _modeRadioToggle.RadioButtonSelected += (sender, index) => {
        if (_storageMonitor != null)
        {
          _storageMonitor.SetMode((ResourceCounterMode)index);
          UpdateFragment();
        }
      };

      _goodDropdown = _root.Q<Dropdown>("Good");
      _measurement = _root.Q<Label>("Measurement");

      _includeInputsToggle = _root.Q<Toggle>("Toggle");

      _includeInputsToggle.style.marginTop = 14;
      _includeInputsToggle.style.marginBottom = 13;

      _includeInputsToggle.RegisterValueChangedCallback(evt => {
        if (_storageMonitor != null)
        {
          _storageMonitor.SetIncludeInputs(evt.newValue);
        }
      });

      var onWrapper = _root.Q<VisualElement>("ComparisonWrapper");
      onWrapper.Q<Dropdown>("ComparisonMode").ToggleDisplayStyle(false);

      _thresholdOnField = onWrapper.Q<IntegerField>("Threshold");
      _fillRateLabelOn = _root.Q<Label>("FillRateLabel");
      _fillRateSliderOn = _root.Q<PreciseSlider>("FillRateSlider");

      _thresholdOnField.isDelayed = true;
      _thresholdOnField.RegisterValueChangedCallback(evt => {
        if (_storageMonitor != null)
        {
          int val = Math.Max(0, evt.newValue);

          _thresholdOnField.SetValueWithoutNotify(val);
          _storageMonitor.SetThresholdOn(val);
        }
      });

      _fillRateSliderOn.SetStepWithoutNotify(0.01f);
      _fillRateSliderOn.SetValueChangedCallback(val => {
        if (_storageMonitor != null)
        {
          _fillRateLabelOn.text = $"{Math.Round(val * 100)}%";
          _storageMonitor.SetFillRateThresholdOn(val);
        }
      });

      var onTitle = new Label(_loc.T(TurnOnIfLocKey));
      onTitle.AddToClassList("game-text-normal");
      onTitle.style.marginTop = 10;
      bottomSection.Insert(bottomSection.IndexOf(onWrapper), onTitle);


      var offTemplate = _visualElementLoader.LoadVisualElement("Game/EntityPanel/ResourceCounterFragment");
      var offWrapper = offTemplate.Q<VisualElement>("ComparisonWrapper");
      offWrapper.Q<Dropdown>("ComparisonMode").ToggleDisplayStyle(false);

      _thresholdOffField = offWrapper.Q<IntegerField>("Threshold");

      _thresholdOffField.style.marginBottom = 13;

      _fillRateLabelOff = offTemplate.Q<Label>("FillRateLabel");
      _fillRateSliderOff = offTemplate.Q<PreciseSlider>("FillRateSlider");

      _thresholdOffField.isDelayed = true;
      _thresholdOffField.RegisterValueChangedCallback(evt => {
        if (_storageMonitor != null)
        {
          int val = Math.Max(0, evt.newValue);

          _thresholdOffField.SetValueWithoutNotify(val);
          _storageMonitor.SetThresholdOff(val);
        }
      });

      _fillRateSliderOff.SetStepWithoutNotify(0.01f);
      _fillRateSliderOff.SetValueChangedCallback(val => {
        if (_storageMonitor != null)
        {
          _fillRateLabelOff.text = $"{Math.Round(val * 100)}%";
          _storageMonitor.SetFillRateThresholdOff(val);
        }
      });

      var offTitle = new Label(_loc.T(TurnOffIfLocKey));
      offTitle.AddToClassList("game-text-normal");
      offTitle.style.marginTop = 10;
      bottomSection.Add(offTitle);
      bottomSection.Add(offWrapper);
      bottomSection.Add(_fillRateLabelOff);
      bottomSection.Add(_fillRateSliderOff);

      _root.ToggleDisplayStyle(false);
      return _root;
    }

    public void ShowFragment(BaseComponent entity)
    {
      _storageMonitor = entity.GetComponent<StorageMonitor>();
      if (_storageMonitor != null)
      {
        _thresholdOnField.SetValueWithoutNotify(_storageMonitor.ThresholdOn);
        _fillRateSliderOn.UpdateValuesWithoutNotify(_storageMonitor.FillRateThresholdOn, 1f);

        _thresholdOffField.SetValueWithoutNotify(_storageMonitor.ThresholdOff);
        _fillRateSliderOff.UpdateValuesWithoutNotify(_storageMonitor.FillRateThresholdOff, 1f);

        _includeInputsToggle.SetValueWithoutNotify(_storageMonitor.IncludeInputs);

        _goodsDropdownProvider = _storageMonitor.GetComponent<StorageMonitorGoodsDropdownProvider>();
        _dropdownItemsSetter.SetItems(_goodDropdown, _goodsDropdownProvider);

        _root.ToggleDisplayStyle(true);
      }
    }

    public void ClearFragment()
    {
      _storageMonitor = null;
      _root.ToggleDisplayStyle(false);
    }

    public void UpdateFragment()
    {
      if (_storageMonitor != null)
      {
        _modeRadioToggle.Update((int)_storageMonitor.Mode);
        _fillRateSliderOn.SetMarker(_storageMonitor.SampledFillRate);
        _fillRateSliderOff.SetMarker(_storageMonitor.SampledFillRate);

        _measurement.text = _storageMonitor.Mode == ResourceCounterMode.StockLevel
            ? _loc.T(MeasurementQuantityLocKey, _storageMonitor.SampledResourceCount)
            : _loc.T(MeasurementPercentLocKey, Math.Round(_storageMonitor.SampledFillRate * 100));

        _fillRateLabelOn.text = $"{Math.Round(_storageMonitor.FillRateThresholdOn * 100)}%";
        _fillRateLabelOff.text = $"{Math.Round(_storageMonitor.FillRateThresholdOff * 100)}%";

        bool isFillRate = _storageMonitor.Mode == ResourceCounterMode.FillRate;

        _fillRateSliderOn.ToggleDisplayStyle(isFillRate);
        _fillRateLabelOn.ToggleDisplayStyle(isFillRate);
        _thresholdOnField.ToggleDisplayStyle(!isFillRate);

        _fillRateSliderOff.ToggleDisplayStyle(isFillRate);
        _fillRateLabelOff.ToggleDisplayStyle(isFillRate);
        _thresholdOffField.ToggleDisplayStyle(!isFillRate);

        _includeInputsToggle.ToggleDisplayStyle(!isFillRate);
      }
    }
  }
}