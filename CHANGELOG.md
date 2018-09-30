# Changelog
All notable changes to this project will be documented in this file.
This project adheres to [Semantic Versioning](http://semver.org/).

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