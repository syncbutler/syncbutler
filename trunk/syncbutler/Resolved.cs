using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SyncButler
{
    public class Resolved
    {
        private ISyncable Left;
        private ISyncable Right;
        private ActionDone Action;

        public Resolved(ISyncable Left, ISyncable Right, ActionDone Action)
        {
            this.Left = Left;
            this.Right = Right;
            this.Action = Action;
        }

        public enum ActionDone
        { DeleteLeft, DeleteRight, DeleteBoth, CopyFromLeft, CopyFromRight, Merged }

        public override string ToString()
        {
            switch (Action)
            {
                case ActionDone.CopyFromLeft:
                    return Left + " copied to " + Right;
                case ActionDone.CopyFromRight:
                    return Right + " copied to " + Left;
                case ActionDone.DeleteBoth:
                    return Left + " and " + Right + " have been deleted";
                case ActionDone.DeleteLeft:
                    return Left + " has been deleted";
                case ActionDone.DeleteRight:
                    return Right + " has been deleted";
                case ActionDone.Merged:
                    return Right + " and " + Left + " have been merged";
            }
            return "No Action has been done for " + Left + " and " + Right; 
        }
    }
}
