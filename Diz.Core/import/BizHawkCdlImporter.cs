﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Diz.Core.model;

namespace Diz.Core.import
{
    public class BizHawkCdlImporter
    {
        private readonly Dictionary<string, IList<BizHawkCdlImporter.Flag>> cdl = new Dictionary<string, IList<Flag>>();
        
        [Flags]
        public enum Flag : byte
        {
            None = 0x00,
            ExecFirst = 0x01,
            ExecOperand = 0x02,
            CPUData = 0x04,
            DMAData = 0x08,
            CPUXFlag = 0x10,
            CPUMFlag = 0x20,
            BRR = 0x80
        }

        public static void Import(string filename, Data data)
        {
            var cdl = new BizHawkCdlImporter();
            cdl.LoadFromFile(filename);
            cdl.CopyInto(data);
        }

        private void LoadFromFile(string path)
        {
            using var fs = new FileStream(path, FileMode.Open);
            LoadFromStream(fs);
        }

        private void LoadFromStream(Stream input)
        {
            var br = new BinaryReader(input);

            string id = br.ReadString();
            string subType = id switch
            {
                "BIZHAWK-CDL-1" => "PCE",
                "BIZHAWK-CDL-2" => br.ReadString().TrimEnd(' '),
                _ => throw new InvalidDataException("File is not a BizHawk CDL file.")
            };

            if (subType != "SNES")
            {
                throw new InvalidDataException("The CDL file is not for SNES.");
            }

            int count = br.ReadInt32();
            for (var i = 0; i < count; i++)
            {
                string key = br.ReadString();
                int len = br.ReadInt32();
                var data = br.ReadBytes(len).Select(b => (Flag)b).ToArray();
                cdl[key] = data;
            }
        }
        private void CopyInto(Data data)
        {
            if (!cdl.TryGetValue("CARTROM", out var cdlRomFlags))
            {
                throw new InvalidDataException("The CDL file does not contain CARTROM block.");
            }

            var size = Math.Min(cdlRomFlags.Count, data.GetROMSize());
            bool m = false;
            bool x = false;
            for (var offset = 0; offset < size; offset++)
            {
                var cdlFlag = cdlRomFlags[offset];
                if (cdlFlag == BizHawkCdlImporter.Flag.None)
                    continue;

                var type = Data.FlagType.Unreached;
                if ((cdlFlag & BizHawkCdlImporter.Flag.ExecFirst) != 0)
                {
                    type = Data.FlagType.Opcode;
                    m = (cdlFlag & BizHawkCdlImporter.Flag.CPUMFlag) != 0;
                    x = (cdlFlag & BizHawkCdlImporter.Flag.CPUXFlag) != 0;
                }
                else if ((cdlFlag & BizHawkCdlImporter.Flag.ExecOperand) != 0)
                    type = Data.FlagType.Operand;
                else if ((cdlFlag & BizHawkCdlImporter.Flag.CPUData) != 0)
                    type = Data.FlagType.Data8Bit;
                else if ((cdlFlag & BizHawkCdlImporter.Flag.DMAData) != 0)
                    type = Data.FlagType.Data8Bit;
                data.Mark(offset, type, 1);

                if (type != Data.FlagType.Opcode && type != Data.FlagType.Operand) 
                    continue;

                // Operand reuses the last M and X flag values used in Opcode,
                // since BizHawk CDL records M and X flags only in Opcode.
                data.MarkMFlag(offset, m, 1);
                data.MarkXFlag(offset, x, 1);
            }
        }
    }
}