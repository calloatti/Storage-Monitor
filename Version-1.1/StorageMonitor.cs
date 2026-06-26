using System;
using Timberborn.Automation;
using Timberborn.AutomationBuildings;
using Timberborn.BaseComponentSystem;
using Timberborn.BlockSystem;
using Timberborn.Coordinates;
using Timberborn.DuplicationSystem;
using Timberborn.EntitySystem;
using Timberborn.Goods;
using Timberborn.InventorySystem;
using Timberborn.Persistence;
using Timberborn.Stockpiles;
using Timberborn.WorldPersistence;
using UnityEngine;

namespace Calloatti.StorageMonitor
{
  public class StorageMonitor : BaseComponent, ISamplingTransmitter, ITransmitter, IPersistentEntity, IDuplicable<StorageMonitor>, IDuplicable, IInitializableEntity, IDeletableEntity
  {
    private static readonly ComponentKey StorageMonitorKey = new ComponentKey("StorageMonitor");
    private static readonly PropertyKey<string> GoodIdKey = new PropertyKey<string>("GoodId");
    private static readonly PropertyKey<ResourceCounterMode> ModeKey = new PropertyKey<ResourceCounterMode>("Mode");
    private static readonly PropertyKey<bool> IncludeInputsKey = new PropertyKey<bool>("IncludeInputs");

    private static readonly PropertyKey<int> ThresholdOnKey = new PropertyKey<int>("ThresholdOn");
    private static readonly PropertyKey<float> FillRateOnKey = new PropertyKey<float>("FillRateOn");

    private static readonly PropertyKey<int> ThresholdOffKey = new PropertyKey<int>("ThresholdOff");
    private static readonly PropertyKey<float> FillRateOffKey = new PropertyKey<float>("FillRateOff");

    private static readonly PropertyKey<bool> CurrentlyActiveKey = new PropertyKey<bool>("CurrentlyActive");

    private readonly IGoodService _goodService;
    private readonly IBlockService _blockService;

    private Automator _automator;

    private string _goodId;
    public string GoodId
    {
      get
      {
        if (string.IsNullOrEmpty(_goodId) && _goodService != null && _goodService.Goods.Count > 0)
        {
          _goodId = _goodService.Goods.Contains("Water") ? "Water" : _goodService.Goods[0];
        }
        return _goodId;
      }
      private set => _goodId = value;
    }

    public ResourceCounterMode Mode { get; private set; }
    public bool IncludeInputs { get; private set; }

    public int ThresholdOn { get; private set; } = 20;
    public float FillRateThresholdOn { get; private set; } = 0.2f;

    public int ThresholdOff { get; private set; } = 100;
    public float FillRateThresholdOff { get; private set; } = 0.8f;

    public int SampledResourceCount { get; private set; }
    public float SampledFillRate { get; private set; }

    private bool _currentlyActive;
    public event EventHandler<string> GoodChanged;

    internal StorageMonitor(IGoodService goodService, IBlockService blockService)
    {
      _goodService = goodService;
      _blockService = blockService;
    }

    public void Awake()
    {
      // No-op: Initialization is now securely handled on-demand by the smart property getter
    }

    public void InitializeEntity()
    {
      _automator = GetComponent<Automator>();
      Sample();
    }

    public void Start()
    {
      _automator?.SetState(_currentlyActive);
    }

    public void DeleteEntity()
    {
      // No-op: No district events to unsubscribe from anymore
    }

    public void Save(IEntitySaver entitySaver)
    {
      var component = entitySaver.GetComponent(StorageMonitorKey);
      component.Set(GoodIdKey, GoodId);
      component.Set(ModeKey, Mode);
      component.Set(IncludeInputsKey, IncludeInputs);

      component.Set(ThresholdOnKey, ThresholdOn);
      component.Set(FillRateOnKey, FillRateThresholdOn);

      component.Set(ThresholdOffKey, ThresholdOff);
      component.Set(FillRateOffKey, FillRateThresholdOff);

      component.Set(CurrentlyActiveKey, _currentlyActive);
    }

    public void Load(IEntityLoader entityLoader)
    {
      if (entityLoader.TryGetComponent(StorageMonitorKey, out var objectLoader))
      {
        if (objectLoader.Has(GoodIdKey))
        {
          string loadedGood = objectLoader.Get(GoodIdKey);
          if (!string.IsNullOrEmpty(loadedGood)) { GoodId = loadedGood; }
        }

        if (objectLoader.Has(ModeKey)) Mode = objectLoader.Get(ModeKey);
        if (objectLoader.Has(IncludeInputsKey)) IncludeInputs = objectLoader.Get(IncludeInputsKey);

        if (objectLoader.Has(ThresholdOnKey)) ThresholdOn = objectLoader.Get(ThresholdOnKey);
        if (objectLoader.Has(FillRateOnKey)) FillRateThresholdOn = objectLoader.Get(FillRateOnKey);

        if (objectLoader.Has(ThresholdOffKey)) ThresholdOff = objectLoader.Get(ThresholdOffKey);
        if (objectLoader.Has(FillRateOffKey)) FillRateThresholdOff = objectLoader.Get(FillRateOffKey);

        if (objectLoader.Has(CurrentlyActiveKey)) _currentlyActive = objectLoader.Get(CurrentlyActiveKey);
      }
    }

    public void DuplicateFrom(StorageMonitor source)
    {
      GoodId = source.GoodId;
      Mode = source.Mode;
      IncludeInputs = source.IncludeInputs;

      ThresholdOn = source.ThresholdOn;
      FillRateThresholdOn = source.FillRateThresholdOn;

      ThresholdOff = source.ThresholdOff;
      FillRateThresholdOff = source.FillRateThresholdOff;

      _currentlyActive = source._currentlyActive;

      InvokeGoodChangeEvent(source.GoodId);
      Sample();
    }

    public void SetGoodId(string goodId) { GoodId = goodId; InvokeGoodChangeEvent(goodId); Sample(); }
    public void SetMode(ResourceCounterMode mode) { Mode = mode; Sample(); }
    public void SetIncludeInputs(bool include) { IncludeInputs = include; Sample(); }

    public void SetThresholdOn(int threshold) { ThresholdOn = threshold; UpdateOutputState(); }
    public void SetFillRateThresholdOn(float fillRate) { FillRateThresholdOn = fillRate; UpdateOutputState(); }

    public void SetThresholdOff(int threshold) { ThresholdOff = threshold; UpdateOutputState(); }
    public void SetFillRateThresholdOff(float fillRate) { FillRateThresholdOff = fillRate; UpdateOutputState(); }

    private Timberborn.InventorySystem.Inventory GetTargetStorage()
    {
      var blockObject = GetComponent<BlockObject>();
      if (blockObject == null) return null;

      // Calculate the coordinate exactly 1 block in the direction of the placement arrow
      Vector3Int offset = blockObject.Placement.Orientation.Transform(new Vector3Int(0, -1, 0));
      Vector3Int targetCoord = blockObject.CoordinatesAtBaseZ + offset;

      var objs = _blockService.GetObjectsAt(targetCoord);
      foreach (var obj in objs)
      {
        if (obj == blockObject) continue;

        var stockpile = obj.GetComponent<Stockpile>();
        if (stockpile != null && stockpile.Inventory != null)
        {
          return stockpile.Inventory;
        }
      }
      return null;
    }

    public void Sample()
    {
      Timberborn.InventorySystem.Inventory targetStorage = GetTargetStorage();

      if (targetStorage != null && targetStorage.Enabled)
      {
        var allower = targetStorage.GetComponent<SingleGoodAllower>();
        string storageGoodId = (allower != null && allower.HasAllowedGood) ? allower.AllowedGood : null;

        if (!string.IsNullOrEmpty(storageGoodId) && GoodId != storageGoodId)
        {
          GoodId = storageGoodId;
          InvokeGoodChangeEvent(storageGoodId);
        }

        if (!string.IsNullOrEmpty(GoodId))
        {
          if (Mode == ResourceCounterMode.StockLevel)
          {
            SampledResourceCount = IncludeInputs ? targetStorage.AmountInStock(GoodId) : targetStorage.UnreservedAmountInStock(GoodId);
          }
          else
          {
            int amount = targetStorage.AmountInStock(GoodId);
            int capacity = targetStorage.Capacity;
            SampledFillRate = capacity > 0 ? (float)amount / capacity : 0f;
          }
        }
        else
        {
          SampledResourceCount = 0;
          SampledFillRate = 0f;
        }
      }
      else
      {
        SampledResourceCount = 0;
        SampledFillRate = 0f;
      }

      UpdateOutputState();
    }

    private void UpdateOutputState()
    {
      bool targetState = _currentlyActive;

      if (Mode == ResourceCounterMode.StockLevel && SampledResourceCount <= ThresholdOn) targetState = true;
      if (Mode == ResourceCounterMode.FillRate && SampledFillRate <= FillRateThresholdOn) targetState = true;

      if (Mode == ResourceCounterMode.StockLevel && SampledResourceCount >= ThresholdOff) targetState = false;
      if (Mode == ResourceCounterMode.FillRate && SampledFillRate >= FillRateThresholdOff) targetState = false;

      if (targetState != _currentlyActive)
      {
        _currentlyActive = targetState;
        _automator?.SetState(_currentlyActive);
      }
    }

    private void InvokeGoodChangeEvent(string goodId)
    {
      this.GoodChanged?.Invoke(this, goodId);
    }
  }
}