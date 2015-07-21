#Source File Elastic Crawler

Utility to import source files into ElasticSearch database.

## Configuration (App.Config)

Under appSettings.

### ServerAddress

ElasticSearch server IP Address or host name

	example: <add key="ServerAddress" value="localhost"/>

### ServerPort

ElasticSearch server port

	  example: <add key="ServerPort" value="9200"/>
	  
### RootDir

Root directory path to start import from. 

* The utility support up to 10 sets of directory and extensions combinations, where each combination is identified by number RootDir1, RootDir2, RootDir3, etc.
* The numbers shell be sequential  

	  example: <add key="RootDir1" value="c:\nimsf"/>
	  
### FilesExt

File extensions to be imported

* The utility support up to 10 sets of directory and extensions combinations, where each combination is identified by number FilesExt1, FilesExt2, FilesExt3, etc.
* The numbers shell be sequential  
	  
	  example: <add key="FilesExt1" value="*.js"/>	


## Error Processing ("error levels")

If the utility is run as part of batch shell script, then two exit codes ("error levels") are supported: 

* ERROR_INVALID_APP_CONFIG = 0x667
* ERROR_DATABASE_ERROR = 0xA0

The powershell script can be something like:

	echo off
	SourceFileElasticCrawler.exe %1
	If errorlevel 1639 goto InvCfg 
	if errorlevel 160 goto DbErr
	goto :EOF

	:InvCfg
	echo Missing configuration
	goto :EOF

	: DbErr
	echo Database Error
	goto :EOF


## Change Log

21.7.2015 - first RC