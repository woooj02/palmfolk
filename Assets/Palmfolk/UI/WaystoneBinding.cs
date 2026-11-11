#if UNITY_5_3_OR_NEWER
using System;
using System.Collections.Generic;
using Palmfolk.Core;
using Palmfolk.Platform;
using UnityEngine;

namespace Palmfolk.UI
{
    // Commands go through the runner, never through the display component or a second simulation.
    public sealed class WaystoneBinding : MonoBehaviour
    {
        [SerializeField] private PalmfolkRunner runner;
        [SerializeField] private WaystoneView view;
        [SerializeField] private Transform head;
        public Action Quit { get; set; }
        public bool LastRequestAccepted { get; private set; }
        private Func<IReadOnlyList<GazeButton>> buttons;
        public void Configure(PalmfolkRunner gameRunner, WaystoneView waystone, Transform viewer = null)
        { Detach(); runner = gameRunner; view = waystone; head = viewer; Attach(); }
        private void OnEnable() => Attach();
        private void OnDisable() => Detach();
        private void Attach()
        {
            if (runner == null || view == null) return;
            if (buttons == null) buttons = Buttons;
            Detach(); view.Requested += Requested; runner.Opened += Reflect; runner.Presented += Reflect;
            runner.GazeSelected += Selected; runner.GazeButtons = buttons;
            if (runner.Game != null) Reflect(runner);
        }
        private void Detach()
        {
            if (view != null) view.Requested -= Requested;
            if (runner == null) return;
            runner.Opened -= Reflect; runner.Presented -= Reflect; runner.GazeSelected -= Selected;
            if (runner.GazeButtons == buttons) runner.GazeButtons = null;
        }
        private IReadOnlyList<GazeButton> Buttons()
        {
            if (view == null || !view.isActiveAndEnabled || runner?.Game == null) return null;
            if (head != null) view.LayoutForViewer(head.position);
            return view.GazeButtons();
        }
        private void Selected(string id)
        { int slot = WaystoneView.Slot(id); if (slot >= 0 && view.isActiveAndEnabled) view.RequestSlot(slot); }
        private void Requested(WaystoneRequest request) => LastRequestAccepted = Handle(request);
        public bool Handle(WaystoneRequest request)
        {
            if (runner?.Game == null) return false;
            InputCommand command;
            switch (request.Action)
            {
                case WaystoneAction.Pause: command = InputCommand.Pause; break;
                case WaystoneAction.Resume: command = InputCommand.Resume; break;
                case WaystoneAction.Restart: command = InputCommand.Restart; break;
                case WaystoneAction.Replant: command = InputCommand.Replant; break;
                case WaystoneAction.Bell: command = InputCommand.RingBell; break;
                case WaystoneAction.Mode:
                    runner.ChooseMode(request.Value == 1 ? ValleyMode.Table : ValleyMode.VR); Reflect(runner); return true;
                case WaystoneAction.Quit:
                    runner.SaveNow(); if (Quit != null) Quit(); else Application.Quit(); return true;
                default:
                    var player = runner.Player.Copy();
                    switch (request.Action)
                    {
                        case WaystoneAction.Stillness: player.StillnessToleranceStep = (int)request.Value; break;
                        case WaystoneAction.Scale: player.WorldScale = request.Value; break;
                        case WaystoneAction.ReducedMotion: player.ReducedMotion = request.Value != 0; break;
                        case WaystoneAction.Captions: player.Captions = request.Value != 0; break;
                        case WaystoneAction.HighContrast: player.HighContrast = request.Value != 0; break;
                        case WaystoneAction.XRay: player.XRay = request.Value != 0; break;
                        default: return false;
                    }
                    player.Validate(); runner.Apply(player); Reflect(runner); return true;
            }
            return runner.Request(command);
        }
        private void Reflect(PalmfolkRunner source)
        {
            if (source.Game == null || view == null) return;
            var p = source.Player;
            view.Reflect(p.Apply(source.BaseSettings), source.Game.IsPaused, p.ReducedMotion, p.Captions, p.HighContrast, p.XRay, source.Flow?.Running == ValleyMode.Table);
            view.SetHovered(source.Pointer.Hovered);
        }
    }
}
#endif
