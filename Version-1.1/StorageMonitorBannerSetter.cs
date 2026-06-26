using System;
using Timberborn.BaseComponentSystem;
using Timberborn.BlockSystem;
using Timberborn.Buildings;
using Timberborn.EntitySystem;
using Timberborn.Goods;
using UnityEngine;

namespace Calloatti.StorageMonitor
{
  public class StorageMonitorBannerSetter : BaseComponent, IAwakableComponent, IInitializablePreview, IInitializableEntity, IDeletableEntity
  {
    private static readonly Color BannerIconColor = new Color(0.33f, 0.33f, 0.33f);

    private readonly GoodIconVisualizer _goodIconVisualizer;
    private readonly IGoodService _goodService;

    private BlockObject _blockObject;
    private StorageMonitor _storageMonitor;
    private MeshRenderer _meshRenderer;

    public StorageMonitorBannerSetter(GoodIconVisualizer goodIconVisualizer, IGoodService goodService)
    {
      _goodIconVisualizer = goodIconVisualizer;
      _goodService = goodService;
    }

    public void Awake()
    {
      _blockObject = GetComponent<BlockObject>();
      _storageMonitor = GetComponent<StorageMonitor>();
      BuildingModel component = GetComponent<BuildingModel>();

      _meshRenderer = component.FinishedModel.GetComponentInChildren<MeshRenderer>();
    }

    public void InitializePreview()
    {
      UpdateProperties();
    }

    public void InitializeEntity()
    {
      _storageMonitor.GoodChanged += OnGoodChanged;
      UpdateProperties();
    }

    public void DeleteEntity()
    {
      if (_storageMonitor != null)
      {
        _storageMonitor.GoodChanged -= OnGoodChanged;
      }
    }

    private void OnGoodChanged(object sender, string e)
    {
      UpdateProperties();
    }

    private void UpdateProperties()
    {
      string goodId = _storageMonitor.GoodId;
      if (string.IsNullOrWhiteSpace(goodId))
      {
        _goodIconVisualizer.HideColoredIcon(_meshRenderer.material);
        return;
      }

      GoodSpec good = _goodService.GetGood(goodId);
      _goodIconVisualizer.ShowColoredIcon(_meshRenderer.material, good, _blockObject.FlipMode.IsFlipped, BannerIconColor);
    }
  }
}