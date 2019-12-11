# Changelog
All notable changes to this project will be documented in this file.
This project adheres to [Semantic Versioning](http://semver.org/).

## [1.1] - 2019-12-11
### Added
- DpiAwareness
- Application ID column
- support for DeploymentAction='OptionalInstallation'


## [1.0] - 2019-10-19
### Added
- Added italian translation thx @irondave
- Added Brazilian Portuguese translation thx @Possessed777
- Added ini option to select language

### Fixed
- fixed minor issues with progress display

### Changed
- date format should now be proeprly localized
- improved auto check for update feature


## [0.9a+] - 2018-12-07
### Added
- Added Russian translation thx @zetcamp

## [0.9a] - 2018-12-06
### Added
- Added Japanese translation thx @Rukoto 
- Added Polish translation thx @vitos
- added select all checkbox

### Fixed
- Fixed auto update crash issue
- date formating in last searche for rupdate log
- fixed date and size sorting issue in columns

### Changed
- now ctrl+f sets cursot to the searhc box
- improved sorting, now sort order can be reversed by clicking agina on the column


## [0.8g beta] - 2018-11-1
### Added
- Added french translation thanks to Leo

### Changed
- now the WU setting is always available, and it not ato set when chagrin AU blocking options.


## [0.8] - 2018-10-23
### Fixed
- issue when uninstalling updates

## [0.8c beta] - 2018-10-21
### Added
- messge box promping for a reboot when changing update facilitator settings
- tooltips to list view for long texts

### Changed
- some buttons are now disabled when no updates are checked

### Fixed
- issue with supprot url's nor manualy generated mased on the kb number


## [0.8b beta] - 2018-10-20
### Added
- command line parameter for scripted operation, disabling configuration options -provisioned
- added search filter ctrl+f
- addec ctrl+c to copy infos about selected updates
- added option to blacklist updates by KB using the updates ini, also collor them or pre select them

Example:
[KB4023307]
BlackList=1
Remove=0
Color=#ffcccc

[KB4343909]
Select=1
Color=#ccffcc

### Changed
- updates are now cached in updates.ini inside teh downloads directory, updates.ini in the working directorty is used for persistent update informations

### Fixed
- fixed typos in transaltion thx to Carlos Detweiller and PointZero


## [0.8a beta] - 2018-10-19
### Added
- translation support

### Fixed
- crash bug in uninstall routile
- size and date columns ware out of order
- fixed some GPO related crash issues


## [0.7] - 2018-10-05
### Added
- option to disable update facilitation services
- ability to "manually" install updates

### Changed
- automatic update GPO handling, now much more user friendly
- reworked error handling to allow limited non admin operation
- reworked status codes for better ui expirience
- when download fails but the file was already downloaded in the previuse session the old file is used
- reworked UAC bypass handling

### Fixed
- windows 10 version detection
- issue when started rom a read only directory, fallback to ...\{UserProfile}\Downloads\WuMgr\
- crash bug when firewall blocks downloads
- issue client not properl abborting operations on cancesss


## [0.6b] - 2018-09-30
### Fixed
- issues only one instance restriction
- issues with list view separation


## [0.6] - 2018-09-30
### Added
- checkbox to hide the WU settings page instead of automatic operation
- when access elevation fails the tool now asks for admin rights
- added tool entry to setup/remove windows defender update task

### Changed
- ObjectListView.dll is not longer required instead a simple self contained control is used
- replaced the app icon with a nicer one

### Fixed
- issue when UAC bypass failed due to restriction to only one instance
- then starting a tool from the menu it sets working directory to the tool's directory
- fixed issues with -onclose now no console window is shown and better "" parsing is implemented


## [0.5] - 2018-09-16
### Added
- wumgr.ini is restricted to be writeble only by administrators
- added better download system, now server date is checked and files get downloaded only if there are newer ont he server
- automatic check for updates
- added support url label
- added customizable tools menu tin better integrate 3rd aprty tools, accessible from tray and the window system menu.
- added update cache such to remembet the last state between application restarts

### Changed
- UAC bypass implementation to prevent possible privilege escalation
- Cleanuped the code base
- auto start option is now called run in background, when closing the window ehen in that mode the application sontinues to run in tray

### Fixed
- fixed size display for kb sized patches
- auto update issue introduced in 0.4
- update list not updating when updates were installed/uninstalled/etc
- two instances cant longer be started at once

## [0.4] - 2018-09-08
### Added
- option to register and unregister microsoft update
- added commandline option -offline [download|no_download|download_new]
- added commandline option -online [serviceID]
- added GPO to block connections to M$ update servers on pro/home SKU's based on the "Windows Restricted Traffic Limited Functionality Baseline"
- added check to switch between "manual" download/instalation (that is done by WuMgr without using windows update facilities) and the usage of wuauserv
- added propepr icons
- automatically hiding the windows update page when update is disabled or access to M$ servers restricted
- added about dialog
- added configuration ini file

### Changed
- improved applog
- improved agent events
- fixed category and state display for history
- unifyed catalog cab and update download
- improved custom update downloader

## [0.3] - 2018-09-02
### Added
- System tray Icon
- Auto Start
- UAC bypass for administrator users
- added warning if running without window supdate service enabled
- added -console command line option to show a debug console 
- added /? command line option to show all available command line options
- added direct update download, i.e. not using the update service
- added propepr slitter for the log
- added settings saving to registry

### Fixed
- multiple errors with offlien update search
- issue with slow history loading

## [0.2] - 2018-08-26
### Added
- Add command line options compatible with wumt (wumgr -update -onclose [command])
- Add option to auto download the *.cab file for semi-offline update
- Finish the GPO groupe

## [0.1] - 2018-08-16
### Added
- Initial release