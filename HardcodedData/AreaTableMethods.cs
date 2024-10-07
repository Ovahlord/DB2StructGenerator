using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DB2StructGenerator.HardcodedData
{
    public class AreaTableMethods : HardcodedMethods
    {
        public AreaTableMethods()
        {
            StorageName = "AreaTable";
            Methods.Add("EnumFlag<AreaFlags> GetFlags() const { return static_cast<AreaFlags>(Flags[0]); }");
            Methods.Add("EnumFlag<AreaFlags2> GetFlags2() const { return static_cast<AreaFlags2>(Flags[1]); }");
            Methods.Add("EnumFlag<AreaMountFlags> GetMountFlags() const { return static_cast<AreaMountFlags>(MountFlags); }");
            Methods.Add("bool IsSanctuary() const { return GetFlags().HasFlag(AreaFlags::NoPvP); }");
        }
    }
}
