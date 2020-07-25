﻿using System;
using System.Collections.Generic;
using System.Linq;

namespace PKHeX.Core
{
    /// <summary>
    /// Generation 8 <see cref="SaveFile"/> object for <see cref="GameVersion.SWSH"/> games.
    /// </summary>
    public sealed class SAV8SWSH : SAV8, ISaveBlock8SWSH, ITrainerStatRecord
    {
        public SAV8SWSH(byte[] data) : this(data, SwishCrypto.Decrypt(data))
        {
        }

        private SAV8SWSH(byte[] data, IReadOnlyList<SCBlock> blocks) : base(data)
        {
            Data = Array.Empty<byte>();
            AllBlocks = blocks;
            Blocks = new SaveBlockAccessor8SWSH(this);
            Initialize();
        }

        public SAV8SWSH()
        {
            AllBlocks = Meta8.GetBlankDataSWSH();
            Blocks = new SaveBlockAccessor8SWSH(this);
            Initialize();
            ClearBoxes();
        }

        public override void CopyChangesFrom(SaveFile sav)
        {
            // Absorb changes from all blocks
            var z = (SAV8SWSH)sav;
            var mine = AllBlocks;
            var newB = z.AllBlocks;
            for (int i = 0; i < mine.Count; i++)
                newB[i].Data.CopyTo(mine[i].Data, 0);
            Edited = true;
        }

        public IReadOnlyList<SCBlock> AllBlocks { get; }
        public override bool ChecksumsValid => true;
        public override string ChecksumInfo => string.Empty;
        protected override void SetChecksums() { } // None!
        protected override byte[] GetFinalData() => SwishCrypto.Encrypt(AllBlocks);

        public override PersonalTable Personal => PersonalTable.SWSH;
        public override IReadOnlyList<ushort> HeldItems => Legal.HeldItems_SWSH;

        #region Blocks
        public SaveBlockAccessor8SWSH Blocks { get; }
        public override Box8 BoxInfo => Blocks.BoxInfo;
        public override Party8 PartyInfo => Blocks.PartyInfo;
        public override MyItem Items => Blocks.Items;
        public override MyStatus8 MyStatus => Blocks.MyStatus;
        public override Misc8 Misc => Blocks.Misc;
        public override Zukan8 Zukan => Blocks.Zukan;
        public override BoxLayout8 BoxLayout => Blocks.BoxLayout;
        public override PlayTime8 Played => Blocks.Played;
        public override Fused8 Fused => Blocks.Fused;
        public override Daycare8 Daycare => Blocks.Daycare;
        public override Record8 Records => Blocks.Records;
        public override TrainerCard8 TrainerCard => Blocks.TrainerCard;
        public override RaidSpawnList8 Raid => Blocks.Raid;
        public override RaidSpawnList8 RaidArmor => Blocks.RaidArmor;
        public override TitleScreen8 TitleScreen => Blocks.TitleScreen;
        public override TeamIndexes8 TeamIndexes => Blocks.TeamIndexes;

        public T GetValue<T>(uint key) where T : struct
        {
            if (!Exportable)
                return default;
            var value = Blocks.GetBlockValue(key);
            if (value is T v)
                return v;
            throw new ArgumentException($"Incorrect type request! Expected {typeof(T).Name}, received {value.GetType().Name}", nameof(T));
        }

        public void SetValue<T>(uint key, T value) where T : struct
        {
            if (!Exportable)
                return;
            Blocks.SetBlockValue(key, value);
        }

        #endregion
        public override SaveFile Clone() => new SAV8SWSH(BAK, AllBlocks.Select(z => z.Clone()).ToArray());

        private int m_spec, m_item, m_move, m_abil;
        public override int MaxMoveID => m_move;
        public override int MaxSpeciesID => m_spec;
        public override int MaxItemID => m_item;
        public override int MaxBallID => Legal.MaxBallID_8;
        public override int MaxGameID => Legal.MaxGameID_8;
        public override int MaxAbilityID => m_abil;

        private void Initialize()
        {
            Box = 0;
            Party = 0;
            PokeDex = 0;
            TeamIndexes.LoadBattleTeams();

            int rev = Zukan.GetRevision();
            if (rev == 0)
            {
                m_move = Legal.MaxMoveID_8_O0;
                m_spec = Legal.MaxSpeciesID_8_O0;
                m_item = Legal.MaxItemID_8_O0;
                m_abil = Legal.MaxAbilityID_8_O0;
            }
            else
            {
                m_move = Legal.MaxMoveID_8_R1;
                m_spec = Legal.MaxSpeciesID_8_R1;
                m_item = Legal.MaxItemID_8_R1;
                m_abil = Legal.MaxAbilityID_8_R1;
            }
        }

        public int GetRecord(int recordID) => Records.GetRecord(recordID);
        public void SetRecord(int recordID, int value) => Records.SetRecord(recordID, value);
        public int GetRecordMax(int recordID) => Records.GetRecordMax(recordID);
        public int GetRecordOffset(int recordID) => Records.GetRecordOffset(recordID);
        public int RecordCount => Record8.RecordCount;

        public override StorageSlotFlag GetSlotFlags(int index)
        {
            int team = Array.IndexOf(TeamIndexes.TeamSlots, index);
            if (team < 0)
                return StorageSlotFlag.None;

            team /= 6;
            var val = (StorageSlotFlag)((int)StorageSlotFlag.BattleTeam1 << team);
            if (TeamIndexes.GetIsTeamLocked(team))
                val |= StorageSlotFlag.Locked;
            return val;
        }

        public override int CurrentBox { get => BoxLayout.CurrentBox; set => BoxLayout.CurrentBox = value; }

        public override bool HasBoxWallpapers => true;
        public override bool HasNamableBoxes => true;

        public override int GetBoxWallpaper(int box)
        {
            if ((uint)box >= BoxCount)
                return box;
            var b = Blocks.GetBlock(SaveBlockAccessor8SWSH.KBoxWallpapers);
            return b.Data[box];
        }

        public override void SetBoxWallpaper(int box, int value)
        {
            if ((uint)box >= BoxCount)
                return;
            var b = Blocks.GetBlock(SaveBlockAccessor8SWSH.KBoxWallpapers);
            b.Data[box] = (byte)value;
        }
    }
}
