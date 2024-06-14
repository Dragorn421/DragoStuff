using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Z64Utils_recreate_avalonia_ui;

// TODO this is 100% copypaste from DListViewerWindowVM for now, adapt, refactor.
public partial class SkeletonViewerWindowViewModel : ObservableObject
{
    private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

    [ObservableProperty]
    public F3DZEX.Render.Renderer? _renderer;
    [ObservableProperty]
    private ObservableCollection<F3DZEX.Command.Dlist> _dLists = new();
    [ObservableProperty]
    private string? _decodeError;
    [ObservableProperty]
    private string? _renderError;

    [ObservableProperty]
    private ObservableCollection<SkeletonViewerLimbNode> _skeletonRootLimbNode = new();

    public class AnimationEntry
    {
        public string Name { get; }
        public Z64.Z64Object.AnimationHolder AnimationHolder { get; }
        public AnimationEntry(string name, Z64.Z64Object.AnimationHolder animationHolder)
        {
            Name = name;
            AnimationHolder = animationHolder;
        }
    }
    [ObservableProperty]
    private ObservableCollection<AnimationEntry> _animationEntries = new();
    /*
    [ObservableProperty]
    private ;
    */

    public SkeletonViewerWindowViewModel()
    {
        PropertyChanged += (sender, e) =>
        {
            switch (e.PropertyName)
            {
                case nameof(Renderer):
                    DLists.Clear();
                    DecodeError = null;
                    RenderError = null;
                    break;
            }
        };
    }

    public void SetSegment(int index, F3DZEX.Memory.Segment segment)
    {
        if (Renderer == null)
            throw new Exception("Renderer is null");

        if (index >= 0 && index < F3DZEX.Memory.Segment.COUNT)
        {
            Renderer.Memory.Segments[index] = segment;

            // TODO redecode dlist, rerender
        }
    }

    public void SetSkeleton(Z64.Z64Object.SkeletonHolder skeletonHolder)
    {
        // TODO most of this should absolutely be in Model

        Debug.Assert(Renderer != null);

        List<Z64.Z64Object.SkeletonLimbHolder> limbHolders;

        {
            byte[] limbsData = Renderer.Memory.ReadBytes(skeletonHolder.LimbsSeg, skeletonHolder.LimbCount * 4);
            var limbsHolder = new Z64.Z64Object.SkeletonLimbsHolder("limbs", limbsData);

            limbHolders = new(limbsHolder.LimbSegments.Length);

            for (int i = 0; i < limbsHolder.LimbSegments.Length; i++)
            {
                byte[] limbData = Renderer.Memory.ReadBytes(limbsHolder.LimbSegments[i], Z64.Z64Object.SkeletonLimbHolder.STANDARD_LIMB_SIZE);
                var limbHolder = new Z64.Z64Object.SkeletonLimbHolder($"limb_{i}", limbData, Z64.Z64Object.EntryType.StandardLimb); // TODO support other limb types

                limbHolders.Add(limbHolder);
            }
        }

        void AddLimbAndSiblingsNodes(int i, List<SkeletonViewerLimbNode> list)
        {
            const byte LIMB_NONE = 0xFF;

            var limbHolder = limbHolders[i];

            List<SkeletonViewerLimbNode> children = new();
            if (limbHolder.Child != LIMB_NONE)
            {
                AddLimbAndSiblingsNodes(limbHolder.Child, children);
            }
            list.Add(new SkeletonViewerLimbNode(limbHolder.Name, children));
            if (limbHolder.Sibling != LIMB_NONE)
            {
                AddLimbAndSiblingsNodes(limbHolder.Sibling, list);
            }
        }

        List<SkeletonViewerLimbNode> root = new();
        AddLimbAndSiblingsNodes(0, root);
        SkeletonRootLimbNode = new() { root.Single() };
    }

    public void SetAnimations(IEnumerable<Z64.Z64Object.AnimationHolder> animationHolders)
    {
        ObservableCollection<AnimationEntry> newAnimations
            = new(animationHolders.Select(animationHolder => new AnimationEntry(animationHolder.Name, animationHolder)));
        AnimationEntries = newAnimations;
    }

    public void SetSingleDlist(uint vaddr)
    {
        if (Renderer == null)
            throw new Exception("Renderer is null");

        Logger.Debug("vaddr={vaddr}", vaddr);

        F3DZEX.Command.Dlist? dList;
        try
        {
            dList = Renderer.GetDlist(vaddr);
        }
        catch (Exception e)
        {
            DecodeError = $"Could not decode DL 0x{vaddr:X8}: {e.Message}";
            dList = null;
        }
        if (dList != null)
        {
            DLists.Clear();
            DLists.Add(dList);
        }
    }

    public void OnAnimationEntrySelected(AnimationEntry animationEntry)
    {
        // TODO compute "jointTable"
        throw new NotImplementedException();
    }
}

public class SkeletonViewerLimbNode
{
    public string Name { get; }
    public ObservableCollection<SkeletonViewerLimbNode> ChildrenLimbs { get; }

    public SkeletonViewerLimbNode(string name, IEnumerable<SkeletonViewerLimbNode>? childrenLimbs = null)
    {
        Name = name;
        ChildrenLimbs = childrenLimbs == null ? new() : new(childrenLimbs);
    }
}
