# WPT
Website Performance Tester.

Features two project:

1. WPTTool - console application with all classes for creating sitemap, parsing web-sites and measuring response time. 
2. WPTWebApp - web application with some kind of friendly UI for website perfrormance testing.

## WPTTool
### Features
* Creating full website map.
  * Including all sub-domens;
  * can create map only from specified address and deeper;
  * can create map with specified number of nodes;
  * can parse or NOT parse params.
* Measuring response time for each page
  * Determine min, max, and average time for each page;
  * make specifiend number of measurments.
* Multithread parsing and measuring.
  * Default number of threads is set by environment number of cores.
  * you can set your own number of threads.
  
  
## WPTWebApp
### Features
* ASP.NET MVC 5 based web-app.
* Angular based front-end aplication.
  * Builds a tree (thanks to [Jaeha Ahn](https://github.com/eu81273) for providing MIT license for [Angular treeview](https://github.com/eu81273/angular.treeview);
  * shows progress of parsing and measuring in realtime;
  * can buid tree in progress (if specified parameter is checked);
* Some Angular Bootstrap based UI.
  * Locks UI when parsing or measurinng to prevent re-building or re-measuring (also got checks if tree is building at back-end).
* Features saving and uploadind tree to/from MSSQL.
  * Can parse and measure uploaded tree.


  
