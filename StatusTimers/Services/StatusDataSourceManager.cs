using StatusTimers.Config;
using StatusTimers.Enums;
using StatusTimers.Interfaces;
using StatusTimers.Logic;
using StatusTimers.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace StatusTimers.Services;

public class StatusDataSourceManager<TKey>(
    IStatusSource<TKey> realStatusSource,
    NodeKind nodeKind,
    Func<bool> getIsPreviewEnabled,
    Func<bool> getShowPermaIcons,
    Func<int> getMaxStatuses,
    Func<int> getItemsPerLine) {
    private readonly Random _rand = new();

    private uint _dummyStatusUniqueId = 1;

    private readonly List<DummyStatusTemplate> _combinedDummyTemplates = [
        new(1239, 213409, "Embolden", 20f, StatusCategory.Buff),
        new(786, 212578, "Battle Litany", 15f, StatusCategory.Buff),
        new(1822, 213709, "Technical Finish", 20f, StatusCategory.Buff),
        new(2174, 212532, "Brotherhood", 15f, StatusCategory.Buff),
        new(2912, 217101, "Vulnerability Up", 30f, StatusCategory.Buff)
    ];

    private readonly List<DummyStatusTemplate> _multiDotDummyTemplates = [
        new(1205, 212616, "Caustic Bite", 45f, StatusCategory.Debuff),
        new(1206, 212617, "Stormbite", 45f, StatusCategory.Debuff),
        new(1228, 213304, "Higanbana", 60, StatusCategory.Debuff),
        new(3871, 212661, "High Thunder", 30, StatusCategory.Debuff)
    ];

    private List<StatusInfo> _dummyActiveStatuses = new();
    private readonly Dictionary<StatusKey, ulong> _dummyGameObjectIdMap = new();
    private DateTime _lastDummyUpdateTime;

    public List<StatusInfo> FetchAndProcessStatuses(StatusTimerOverlayConfig overlayConfig) {
        IReadOnlyList<StatusInfo> current;

        if (getIsPreviewEnabled()) {
            if (_dummyActiveStatuses.Count == 0 || _dummyActiveStatuses.Any(s => s.Id == 0)) {
                InitializeDummyStatuses();
            }

            UpdateDummyStatusTimers();
            current = _dummyActiveStatuses;
        }
        else {
            current = realStatusSource.Fetch(overlayConfig);
            if (_dummyActiveStatuses.Count != 0) {
                _dummyActiveStatuses.Clear();
            }
        }

        IEnumerable<StatusInfo> filteredStatuses = current;
        if (nodeKind == NodeKind.Combined && !getShowPermaIcons()) {
            filteredStatuses = filteredStatuses.Where(s => !s.IsPermanent);
        }

        if (overlayConfig.FilterEnabled && overlayConfig.FilterList is { Count: > 0 }) {
            filteredStatuses = overlayConfig.FilterIsBlacklist
                ? filteredStatuses.Where(s => !overlayConfig.FilterList.Contains(s.Id))
                : filteredStatuses.Where(s => overlayConfig.FilterList.Contains(s.Id));
        }

        return filteredStatuses.Take(getMaxStatuses()).ToList();;
    }

    private void InitializeDummyStatuses() {
        _dummyActiveStatuses.Clear();
        for (int i = 0; i < 30; i++) {
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
                updatedList.Add(CreateNewDummyStatus());
            }
            else {
                updatedList.Add(new StatusInfo(
                    status.Id, status.IconId, status.Name,
                    status.Description, newRemaining, status.MaxSeconds, status.GameObjectId,
                    status.SelfInflicted, status.Stacks, status.PartyPriority,
                    status.IsPermanent, status.ActorName, status.EnemyLetter, status.StatusType
                ));
            }
        }

        _dummyActiveStatuses = updatedList;
    }


    private StatusInfo CreateNewDummyStatus(int? initialIndex = null, uint? id = null) {
        uint dummyId;
        uint dummyIconId;
        string dummyName;
        string dummyDescription;
        float maxSeconds;
        bool isPermanent;
        bool selfInflicted;
        StatusCategory statusCategory;

        uint dummyStacks = (uint)_rand.Next(1, 4);

        ulong gameObjectIdToUse;
        string? actorName;
        char? enemyLetter;

        DummyStatusTemplate selectedTemplate;

        ulong baseActorId = 1000UL;

        if (nodeKind == NodeKind.MultiDoT) {
            if (initialIndex.HasValue) {
                int index = initialIndex.Value;
                selectedTemplate = _multiDotDummyTemplates[index % _multiDotDummyTemplates.Count];
                gameObjectIdToUse = baseActorId + (ulong)index;
                actorName = $"Enemy {gameObjectIdToUse - baseActorId}";
                enemyLetter = (char)('' + (int)(gameObjectIdToUse - baseActorId));
                isPermanent = selectedTemplate.IsPermanent;
                statusCategory = selectedTemplate.StatusType;
            }
            else {
                selectedTemplate = _multiDotDummyTemplates[_rand.Next(0, _multiDotDummyTemplates.Count)];
                int numDummyActors = getMaxStatuses() / getItemsPerLine();
                gameObjectIdToUse = baseActorId + (ulong)_rand.Next(0, Math.Max(1, 30));
                actorName = $"Enemy {gameObjectIdToUse - baseActorId}";
                enemyLetter = (char)('' + (int)(gameObjectIdToUse - baseActorId));
                isPermanent = selectedTemplate.IsPermanent;
                statusCategory = selectedTemplate.StatusType;
            }
        }
        else {
            if (initialIndex.HasValue) {
                int index = initialIndex.Value;
                selectedTemplate = _combinedDummyTemplates[index % _combinedDummyTemplates.Count];
                actorName = null;
                enemyLetter = null;
                isPermanent = selectedTemplate.IsPermanent && index % 5 == 0;
                statusCategory = selectedTemplate.StatusType;
            }
            else {
                selectedTemplate = _combinedDummyTemplates[_rand.Next(0, _combinedDummyTemplates.Count)];
                actorName = null;
                enemyLetter = null;
                isPermanent = selectedTemplate.IsPermanent;
                statusCategory = selectedTemplate.StatusType;
            }
        }

        dummyId = id ?? _dummyStatusUniqueId++;
        gameObjectIdToUse = baseActorId + dummyId;
        dummyIconId = selectedTemplate.IconId;
        dummyName = selectedTemplate.Name;

        dummyDescription = $"Dummy description for {dummyName}";
        maxSeconds = selectedTemplate.MaxSeconds;
        selfInflicted = _rand.Next(100) < 50;

        float remainingSeconds = maxSeconds * (float)(_rand.NextDouble() * 0.8 + 0.1);

        return new StatusInfo(
            dummyId, dummyIconId, dummyName, dummyDescription, remainingSeconds, maxSeconds,
            gameObjectIdToUse, selfInflicted, dummyStacks, 0, // PartyPriority defaults to 0 for dummies
            isPermanent, actorName, enemyLetter, statusCategory
        );
    }

    public TKey KeyOf(StatusInfo info) {
        return realStatusSource.KeyOf(info);
    }
}
