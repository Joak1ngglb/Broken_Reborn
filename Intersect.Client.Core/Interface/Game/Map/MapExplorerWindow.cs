using System;
using System.Collections.Generic;
using System.Linq;
using Intersect.Client.Core;
using Intersect.Client.Framework.File_Management;
using Intersect.Client.Framework.Gwen.Control;
using Intersect.Client.Framework.Gwen.Control.EventArguments;
using Intersect.Client.Framework.Gwen.Control.EventArguments.InputSubmissionEvent;
using Intersect.Client.Framework.Gwen.ControlInternal;
using Intersect.Client.General;
using Intersect.Client.Interface.Shared;
using Intersect.Client.Localization;
using Intersect.Client.Maps;

namespace Intersect.Client.Interface.Game;

public partial class MapExplorerWindow : WindowControl
{
    private readonly ListBox _mapList;
    private readonly ListBox _markerList;
    private readonly TextBox _markerName;
    private readonly TextBoxNumeric _markerX;
    private readonly TextBoxNumeric _markerY;
    private readonly Label _mapSummary;
    private readonly Button _addMarkerButton;
    private readonly Button _removeMarkerButton;

    private Guid _selectedMapId = Guid.Empty;

    public MapExplorerWindow(Canvas gameCanvas) : base(gameCanvas, Strings.MapExplorer.Title)
    {
        Name = nameof(MapExplorerWindow);
        SetSize(780, 480);
        DisableResizing();

        _mapSummary = new Label(this, nameof(_mapSummary))
        {
            FontName = "sourcesansproblack",
            FontSize = 12,
            TextColor = Color.White,
            Text = Strings.MapExplorer.MapSummary.Format(0)
        };
        _mapSummary.SetBounds(16, 8, 360, 24);

        _mapList = new ListBox(this, nameof(_mapList))
        {
            FontName = "sourcesansproblack",
            FontSize = 12,
            TextColor = Color.White,
            AllowMultiSelect = false
        };
        _mapList.SetBounds(16, 36, 330, 400);
        _mapList.EnableScroll(false, true);
        _mapList.RowSelected += MapSelected;

        var markerTitle = new Label(this, "MarkerTitle")
        {
            FontName = "sourcesansproblack",
            FontSize = 12,
            TextColor = Color.White,
            Text = Strings.MapExplorer.Markers
        };
        markerTitle.SetBounds(366, 8, 360, 24);

        _markerList = new ListBox(this, nameof(_markerList))
        {
            FontName = "sourcesansproblack",
            FontSize = 12,
            TextColor = Color.White,
            AllowMultiSelect = false
        };
        _markerList.SetBounds(366, 36, 380, 260);
        _markerList.EnableScroll(false, true);
        _markerList.RowSelected += MarkerSelected;

        var markerNameLabel = new Label(this, "MarkerNameLabel")
        {
            FontName = "sourcesansproblack",
            FontSize = 11,
            Text = Strings.MapExplorer.MarkerLabel
        };
        markerNameLabel.SetBounds(366, 310, 180, 18);

        _markerName = new TextBox(this, nameof(_markerName))
        {
            FontName = "sourcesansproblack",
            FontSize = 11,
            PlaceholderText = Strings.MapExplorer.MarkerPlaceholder
        };
        _markerName.SetBounds(366, 330, 180, 24);

        var coordsLabel = new Label(this, "CoordsLabel")
        {
            FontName = "sourcesansproblack",
            FontSize = 11,
            Text = Strings.MapExplorer.CoordinatesLabel
        };
        coordsLabel.SetBounds(366, 362, 180, 18);

        _markerX = new TextBoxNumeric(this, nameof(_markerX))
        {
            FontName = "sourcesansproblack",
            FontSize = 11,
        };
        _markerX.SetBounds(366, 382, 80, 24);

        _markerY = new TextBoxNumeric(this, nameof(_markerY))
        {
            FontName = "sourcesansproblack",
            FontSize = 11,
        };
        _markerY.SetBounds(466, 382, 80, 24);

        _addMarkerButton = new Button(this, nameof(_addMarkerButton))
        {
            Text = Strings.MapExplorer.AddMarker,
        };
        _addMarkerButton.SetBounds(366, 416, 180, 32);
        _addMarkerButton.Clicked += (_, _) => AddMarker();

        _removeMarkerButton = new Button(this, nameof(_removeMarkerButton))
        {
            Text = Strings.MapExplorer.RemoveMarker,
            IsDisabled = true,
        };
        _removeMarkerButton.SetBounds(566, 416, 180, 32);
        _removeMarkerButton.Clicked += (_, _) => RemoveSelectedMarker();

        LoadJsonUi(GameContentManager.UI.InGame, Graphics.Renderer.GetResolutionString());
        SetPosition(Graphics.Renderer.ScreenWidth / 2 - Width / 2, Graphics.Renderer.ScreenHeight / 2 - Height / 2);

        Hide();
        RefreshMapList();
    }

    public void RefreshMapList()
    {
        _mapList.Clear();
        _selectedMapId = Guid.Empty;
        _markerList.Clear();
        _removeMarkerButton.Disable();

        var grid = Globals.MapGrid;
        var mapEntries = new List<(MapInstance map, int x, int y)>();

        if (grid != null)
        {
            var width = grid.GetLength(0);
            var height = grid.GetLength(1);

            for (var x = 0; x < width; x++)
            {
                for (var y = 0; y < height; y++)
                {
                    var mapId = grid[x, y];
                    if (mapId == Guid.Empty)
                    {
                        continue;
                    }

                    var map = MapInstance.Get(mapId);
                    if (map != null)
                    {
                        mapEntries.Add((map, x, y));
                    }
                }
            }
        }

        foreach (var (map, x, y) in mapEntries.OrderBy(entry => entry.map.Name))
        {
            var markerCount = MapMarkerManager.CountForMap(map.Id);
            var label = Strings.MapExplorer.MapLine.Format(map.Name, x, y, markerCount);
            var row = _mapList.AddRow(label);
            row.UserData = map.Id;
        }

        _mapSummary.Text = Strings.MapExplorer.MapSummary.Format(mapEntries.Count);
        if (mapEntries.Count == 0)
        {
            _mapList.AddRow(Strings.MapExplorer.NoMapsAvailable);
            _mapList.Disable();
            _addMarkerButton.Disable();
        }
        else
        {
            _mapList.Enable();
            _addMarkerButton.Enable();
        }
    }

    public void RefreshMarkerList()
    {
        _markerList.Clear();
        _removeMarkerButton.Disable();

        if (_selectedMapId == Guid.Empty)
        {
            _markerList.AddRow(Strings.MapExplorer.SelectMapPrompt);
            _markerList.Disable();
            return;
        }

        var markers = MapMarkerManager.GetMarkers(_selectedMapId);
        if (markers.Count == 0)
        {
            _markerList.AddRow(Strings.MapExplorer.NoMarkers);
            _markerList.Disable();
            return;
        }

        foreach (var marker in markers.OrderBy(m => m.CreatedUtc))
        {
            var row = _markerList.AddRow(Strings.MapExplorer.MarkerLine.Format(marker.Label, marker.X, marker.Y));
            row.UserData = marker;
        }

        _markerList.Enable();
    }

    private void MapSelected(Base sender, ItemSelectedEventArgs arguments)
    {
        if (arguments.SelectedItem?.UserData is Guid mapId)
        {
            _selectedMapId = mapId;
            RefreshMarkerList();
        }
    }

    private void MarkerSelected(Base sender, ItemSelectedEventArgs arguments)
    {
        _removeMarkerButton.IsDisabled = arguments.SelectedItem?.UserData is not MapMarker;
    }

    private void AddMarker()
    {
        if (_selectedMapId == Guid.Empty)
        {
            new InputBox(Strings.MapExplorer.Title, Strings.MapExplorer.SelectMapPrompt, InputType.OkOnly);
            return;
        }

        var label = _markerName.Text?.Trim();
        var x = (int)_markerX.Value;
        var y = (int)_markerY.Value;

        MapMarkerManager.AddMarker(_selectedMapId, label ?? string.Empty, x, y);
        RefreshMarkerList();
        RefreshMapList();
        _markerName.SetText(string.Empty);
    }

    private void RemoveSelectedMarker()
    {
        if (_markerList.SelectedRow?.UserData is not MapMarker marker)
        {
            return;
        }

        if (MapMarkerManager.RemoveMarker(marker.Id))
        {
            RefreshMarkerList();
            RefreshMapList();
        }
    }
}
