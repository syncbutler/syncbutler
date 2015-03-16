# Introduction #

This page describes in detail the steps taken while scanning Files and Folders. Scans are defined for partnerships, where a partnership is defined as two topmost files/folders (denoted left and right) and an associated dictionary of checksums.

# Preconditions #
  * The topmost objects must be of the same type. (ie. cannot sync a file with a folder)

# Algorithm (Folders) #
  1. Update the drive letters of left and right. This is necessary to ensure that paths to removable devices are not broken by the removable drive being assigned a different drive letter.
  1. If either left or right does not exist on the filesystem, create it.
  1. Iterate through all folders (including sub-folders) in the left path.
    1. For each immediate sub-folder on the left, check that the corresponding folder exists on the right. If the folder does not exist then:
      * If a checksum exists in the dictionary, report that the folder was deleted from the right and suggest to delete the left.
      * Otherwise report that the folder was created on the left and suggest to copy it to the right.
    1. For each immediate sub-folder in the corresponding folder on the right, check that the corresponding folder exists on the left. If it does not exist:
      * If a checksum exists in the dictionary, report that the folder was deleted from the left and suggest to delete the right.
      * Otherwise report that the folder was created on the right and suggest to copy it to the left.
    1. For each file in this current folder on the left, see if the corresponding file exists on the right. If it exists, execute the file scanning algorithm described in the next section. Otherwise:
      * If a checksum exists in the dictionary, report that the file was deleted from the right and suggest to delete the one on the left.
      * Otherwise report it as a new file and suggest to copy it to the right.
    1. For each file in this current folder on the right, see if the corresponding file exists on the left. If it does not exist:
      * If a checksum exists in the dictionary, report that the file was deleted from the left and suggest to delete the one on the right.
      * Otherwise report it as a new file and suggest to copy it to the left.
## Iteration Process ##
As each immediate folder is scanned, if it exists in both left and right paths, it is added into a queue. A loop continuously dequeues a folder from this queue and processes it. When the queue is empty, the iteration is done.

# Algorithm (Files) #
  1. **Update the drive letters of left and right.** This is necessary to ensure that paths to removable devices are not broken by the removable drive being assigned a different drive letter.
  1. Resolve the sync using the algorithms described in [Syncing Algorithms](SyncingAlgorithm.md)