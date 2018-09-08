# Changelog
All notable changes to this project will be documented in this file.
This project adheres to [Semantic Versioning](http://semver.org/).

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