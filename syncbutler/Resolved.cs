using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SyncButler
{
    public class Resolved
    {
        private ISyncable left;
        private ISyncable right;
        private ActionDone action;

        public String Left
        {
            get
            {
                if (action == ActionDone.DeleteRight)
                    return "-";
                return this.left.ToString();
            }
            set
            {

            }
        }
        public string Right
        {
            get
            {
                if (action == ActionDone.DeleteLeft)
                    return "-";
                return this.right.ToString();
            }
            set
            {

            }
        }

        public String Action
        {
            get
            {
                switch (action)
                {
                    case ActionDone.CopyFromLeft:
                        return "copied to";
                    case ActionDone.CopyFromRight:
                        return "copied from";
                    case ActionDone.DeleteBoth:
                        return "deleted with";
                    case ActionDone.DeleteLeft:
                    case ActionDone.DeleteRight:
                        return "deleted";
                    case ActionDone.Merged:
                        return "merged with";
                }
                return "no action done";

            }
            set
            {

            }
        }

        public Resolved(ISyncable left, ISyncable right, ActionDone action)
        {
            this.left = left;
            this.right = right;
            this.action = action;
        }

        public enum ActionDone
        { DeleteLeft, DeleteRight, DeleteBoth, CopyFromLeft, CopyFromRight, Merged, Ignored }

        public override string ToString()
        {
            switch (action)
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
