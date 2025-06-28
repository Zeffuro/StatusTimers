using StatusTimers.Config;
using StatusTimers.Enums;
using StatusTimers.Interfaces;
using StatusTimers.Logic;
using StatusTimers.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace StatusTimers.Services;

public class StatusDataSourceManager<TKey> {
    private static readonly Random _rand = new();

    private static readonly List<DummyStatusTemplate> CombinedDummyTemplates = [
        new(1239, 213409, "Embolden", 20f, StatusCategory.Buff),
        new(786, 212578, "Battle Litany", 15f, StatusCategory.Buff),
        new(1822, 213709, "Technical Finish", 20f, StatusCategory.Buff),
        new(2174, 212532, "Brotherhood", 15f, StatusCategory.Buff),
        new(2912, 217101, "Vulnerability Up", 30f, StatusCategory.Buff)
    ];

    private static readonly List<DummyStatusTemplate> MultiDotDummyTemplates = new() {
        new DummyStatusTemplate(1205, 212616, "Caustic Bite", 45f, StatusCategory.Debuff),
        new DummyStatusTemplate(1206, 212617, "Stormbite", 45f, StatusCategory.Debuff),
        new DummyStatusTemplate(1228, 213304, "Higanbana", 60, StatusCategory.Debuff),
        new DummyStatusTemplate(3871, 212661, "High Thunder", 30, StatusCategory.Debuff)
    };

    private readonly Func<bool> _getIsPreviewEnabled;
    private readonly Func<int> _getItemsPerLine;
    private readonly Func<int> _getMaxStatuses;
    private readonly Func<SortCriterion> _getPrimarySort;
    private readonly Func<SortOrder> _getPrimarySortOrder;
    private readonly Func<SortCriterion> _getSecondarySort;
    private readonly Func<SortOrder> _getSecondarySortOrder;
    private readonly Func<bool> _getShowPermaIcons;
    private readonly Func<SortCriterion> _getTertiarySort;
    private readonly Func<SortOrder> _getTertiarySortOrder;

    private readonly NodeKind _nodeKind;

    private readonly IStatusSource<TKey> _realStatusSource;

    private List<StatusInfo> _dummyActiveStatuses = new();
    private DateTime _lastDummyUpdateTime;

    public StatusDataSourceManager(
        IStatusSource<TKey> realStatusSource,
        NodeKind nodeKind,
        Func<bool> getIsPreviewEnabled,
        Func<bool> getShowPermaIcons,
        Func<int> getMaxStatuses,
        Func<int> getItemsPerLine,
        Func<SortCriterion> getPrimarySort,
        Func<SortOrder> getPrimarySortOrder,
        Func<SortCriterion> getSecondarySort,
        Func<SortOrder> getSecondarySortOrder,
        Func<SortCriterion> getTertiarySort,
        Func<SortOrder> getTertiarySortOrder) {
        _realStatusSource = realStatusSource;
        _nodeKind = nodeKind;
        _getIsPreviewEnabled = getIsPreviewEnabled;
        _getShowPermaIcons = getShowPermaIcons;
        _getMaxStatuses = getMaxStatuses;
        _getItemsPerLine = getItemsPerLine;
        _getPrimarySort = getPrimarySort;
        _getPrimarySortOrder = getPrimarySortOrder;
        _getSecondarySort = getSecondarySort;
        _getSecondarySortOrder = getSecondarySortOrder;
        _getTertiarySort = getTertiarySort;
        _getTertiarySortOrder = getTertiarySortOrder;
    }

    public List<StatusInfo> FetchAndProcessStatuses(StatusTimerOverlayConfig overlayConfig) {
        IReadOnlyList<StatusInfo> current;

        if (_getIsPreviewEnabled()) {
            if (_dummyActiveStatuses.Count == 0 || _dummyActiveStatuses.Any(s => s.Id == 0)) {
                InitializeDummyStatuses();
            }

            UpdateDummyStatusTimers();
            current = _dummyActiveStatuses;
        }
        else {
            current = _realStatusSource.Fetch(overlayConfig);
            if (_dummyActiveStatuses.Any()) {
                _dummyActiveStatuses.Clear();
            }
        }

        IEnumerable<StatusInfo> filteredStatuses = current;
        if (_nodeKind == NodeKind.Combined && !_getShowPermaIcons()) {
            filteredStatuses = filteredStatuses.Where(s => !s.IsPermanent);
        }

        if (overlayConfig.FilterEnabled && overlayConfig.FilterList is { Count: > 0 }) {
            filteredStatuses = overlayConfig.FilterIsBlacklist ? filteredStatuses.Where(s => !overlayConfig.FilterList.Contains(s.Id)) : filteredStatuses.Where(s => overlayConfig.FilterList.Contains(s.Id));
        }

        List<StatusInfo> finalSortedList = StatusSorter.ApplyAllSorts(
            filteredStatuses,
            _getPrimarySort(), _getPrimarySortOrder(),
            _getSecondarySort(), _getSecondarySortOrder(),
            _getTertiarySort(), _getTertiarySortOrder()
        ).Take(_getMaxStatuses()).ToList();

        return finalSortedList;
    }

    private void InitializeDummyStatuses() {
        _dummyActiveStatuses.Clear();
        for (int i = 0; i < _getMaxStatuses(); i++) {
            _dummyActiveStatuses.Add(CreateNewDummyStatus(i));
        }

        _lastDummyUpdateTime = DateTime.Now;
    }

    private void UpdateDummyStatusTimers() {
        TimeSpan elapsed = DateTime.Now - _lastDummyUpdateTime;
        _lastDummyUpdateTime = DateTime.Now;
        float deltaSeconds = (float)elapsed.TotalSeconds;

        List<StatusInfo> updatedList = new();

        foreach (StatusInfo status in _dummyActiveStatuses) {
            if (status.IsPermanent) {
                updatedList.Add(status);
                continue;
            }

            float newRemaining = status.RemainingSeconds - deltaSeconds;
            if (newRemaining <= 0) {
                updatedList.Add(CreateNewDummyStatus()); // Replace with a new dummy status
            }
            else {
                updatedList.Add(new StatusInfo(
                    status.Id, status.IconId, status.Name,
                    newRemaining, status.MaxSeconds, status.GameObjectId,
                    status.SelfInflicted, status.Stacks, status.PartyPriority,
                    status.IsPermanent, status.ActorName, status.EnemyLetter, status.StatusType
                ));
            }
        }

        _dummyActiveStatuses = updatedList;
    }


    private StatusInfo CreateNewDummyStatus(int? initialIndex = null) {
        uint dummyId;
        uint dummyIconId;
        string dummyName;
        float maxSeconds;
        bool isPermanent;
        bool selfInflicted;
        StatusCategory statusCategory;

        uint dummyStacks = (uint)_rand.Next(1, 4);

        ulong gameObjectIdToUse = 0UL;
        string? actorName = null;
        char? enemyLetter = null;

        DummyStatusTemplate selectedTemplate;

        if (_nodeKind == NodeKind.MultiDoT) {
            if (initialIndex.HasValue) {
                int index = initialIndex.Value;
                selectedTemplate = MultiDotDummyTemplates[index % MultiDotDummyTemplates.Count];

                ulong baseActorId = 1000UL;
                gameObjectIdToUse = baseActorId + (ulong)(index / _getItemsPerLine()); // Use _getItemsPerLine()
                actorName = $"Enemy {gameObjectIdToUse - baseActorId}";
                enemyLetter = (char)('' + (int)(gameObjectIdToUse - baseActorId));
                isPermanent = selectedTemplate.IsPermanent;
                statusCategory = selectedTemplate.StatusType;
            }
            else {
                selectedTemplate = MultiDotDummyTemplates[_rand.Next(0, MultiDotDummyTemplates.Count)];

                ulong baseActorId = 1000UL;
                int numDummyActors =
                    _getMaxStatuses() / _getItemsPerLine(); // Use _getMaxStatuses(), _getItemsPerLine()
                gameObjectIdToUse = baseActorId + (ulong)_rand.Next(0, Math.Max(1, numDummyActors));
                actorName = $"Enemy {gameObjectIdToUse - baseActorId}";
                enemyLetter = (char)('' + (int)(gameObjectIdToUse - baseActorId));
                isPermanent = selectedTemplate.IsPermanent;
                statusCategory = selectedTemplate.StatusType;
            }
        }
        else {
            if (initialIndex.HasValue) {
                int index = initialIndex.Value;
                selectedTemplate = CombinedDummyTemplates[index % CombinedDummyTemplates.Count];

                gameObjectIdToUse = 0UL;
                actorName = null;
                enemyLetter = null;
                isPermanent = selectedTemplate.IsPermanent && index % 5 == 0;
                statusCategory = selectedTemplate.StatusType;
            }
            else {
                selectedTemplate = CombinedDummyTemplates[_rand.Next(0, CombinedDummyTemplates.Count)];

                gameObjectIdToUse = 0UL;
                actorName = null;
                enemyLetter = null;
                isPermanent = selectedTemplate.IsPermanent;
                statusCategory = selectedTemplate.StatusType;
            }
        }

        dummyId = selectedTemplate.Id;
        dummyIconId = selectedTemplate.IconId;
        dummyName = selectedTemplate.Name;
        maxSeconds = selectedTemplate.MaxSeconds;
        selfInflicted = _rand.Next(100) < 50;

        float remainingSeconds = maxSeconds * (float)(_rand.NextDouble() * 0.8 + 0.1);

        return new StatusInfo(
            dummyId, dummyIconId, dummyName, remainingSeconds, maxSeconds,
            gameObjectIdToUse, selfInflicted, dummyStacks, 0, // PartyPriority defaults to 0 for dummies
            isPermanent, actorName, enemyLetter, statusCategory
        );
    }

    public TKey KeyOf(StatusInfo info) {
        return _realStatusSource.KeyOf(info);
    }
}
