# Introduction #

Sync Butler automates many of the syncing decisions on behalf of user. There are 2 major concepts in syncing - AutoSync and SuggestAction

## AutoSync ##
Sync Butler keeps dictionary of checksums for all the files in the partnership, it maintains the state of the last successful sync. Using that, AutoSync will be able to resolve 3 types of issue automatically :

  1. New file on one side (assumes the other side no change)
  1. File deleted on one side (assumes the other side no change)
  1. File changed one side (assumes the other side no change)

This is done because we can compare the two states (folder 1 and folder 2) with the last known successful sync. It should be noted that if the sync is happening for the **first** time, there is no dictionary to speak of. In this case, its default behavior is:

  1. Propagate new files to both side
    * No deletions will be carried out automatically in this situation
  1. If there are two modified files with the same filename and path
    * A SuggestAction will be triggered (read below)
  1. If the files are exactly the same, their hash will be added to the dictionary

### Adding to the Checksum Dictionary ###

Checksums for files will be added to the dictionary if the sync performed on that particular file is successful or if two identical files are found during scanning. We only keep one copy of the hash as the files synced will be exactly the same anyway.

Ignore lists for conflicted copies is being considered for future versions.

## SuggestAction ##

This state is involved when no automated decision can be made. It will suggest a default action and display this suggestion to the user during conflict resolution. This is based on the following matrix:
  1. If it is time modified later and larger, it is suggested as the correct action
  1. If it is time modified later but same (or smaller) size, it is suggested as the correct action
  1. If it is time modified about the same time but larger in size, it is suggested as the correct action
  1. None suggested if time modified and size are the same

```
Matrix Reference:
(Column: Size | Row: Time Modified)
         Smaller  Same  Larger
Later       2      2      1
Same        X      4      3
Earlier     X      X      X
```


---


# Folder Sync Algorithm #
![http://syncbutler.org/WindowsFolderSync.png](http://syncbutler.org/WindowsFolderSync.png)


---


# File Sync Algorithm #
![http://syncbutler.org/WindowsFileSync.png](http://syncbutler.org/WindowsFileSync.png)