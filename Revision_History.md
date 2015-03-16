# v0.0 #
This version is not publicly released.

  * Rough UI
  * Very basic synchronisation functionalities (without conflict checking)
  * Able to create/view partnership
  * Sneak Preview of sync butler, sync!

# v0.4 #
This version is not publicly released.

  * UI further developed
  * Synchronisation functionality stabilised and basic support for conflict resolution introduced
  * Proof-of-concept favourites introduced

# v0.6 #
This version is not publicly released.

  * UI further developed and more complete. Introduces common message dialogue
  * Complete partnership viewing feature
  * Syncing is multi threaded
    * Syncing is performed autonomously for very large percentage of the differences in folders
    * Conflicts are presented to the user while syncing is carried, use is able to specify desired action
  * Settings page introduced
  * Refined Sync Butler, Sync! (SBS)
  * More rigourous checks done on behalf of user, smarter predetermination of action for conflicts
    * Included recommended action for conflicts (differences)

# v0.9 #
This version is released for a bug bash.
  * Welcome screen introduced
  * Mostly similar to v0.6

# v1.0 #
This version is released publicly
  * UI improved!
    * More user friendly text and errors
    * Usage of more multi-threads to reduce the lag
    * Help screens added
    * Various other touch ups on different screens such as View Partnerships and conflict resolution.
  * Bugs detected from v0.9 resolved
  * Added more automated tests
  * Improved boundary checks for invalid folders
  * Sync Butler, Sync! feature is no longer allowed to synchronise to a local disk.
  * Various back-end logic refined.