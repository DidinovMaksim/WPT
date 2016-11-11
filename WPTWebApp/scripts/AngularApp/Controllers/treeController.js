WPTngApp.controller('treeController', function ($scope, $http, $interval, $timeout) {

    $scope.parseTree = function () {

        $http({
            url: '/home/ParseTree',
            method: 'GET',
            params: {
                url: $scope.url,
                checkOnlyDeeper: $scope.checkOnlyDeeper,
                parseParams: $scope.parseParams,
                measureAfterward: $scope.measureAfterward,
                limit: $scope.limit
            }
        }).success(function (data, status, headers, config) {
            if (data === "True") {

                $scope.isWorking = true;
                $scope.curAction = "parse";
                $scope.getProgress();
            }

            //$scope.color = data;
        }).error(function () { });
    }
    $scope.measureSpeed = function () {
        $http({
            url: '/home/MeasureSpeed',
            method: 'GET',
        }).success(function (data, status, headers, config) {
            $scope.isWorking = true;
            $scope.curAction = "measure";
            $scope.getProgress();
        });
    }
    $scope.getSlowestNode = function () {
        $http({
            url: '/home/GetSlowestNode',
            method: 'GET',
        }).success(function (data, status, headers, config) {
            $scope.slowestNode = data.Display;
            $scope.showSlowestNode = true;
        });
    }
    $scope.getFastestNode = function () {
        $http({
            url: '/home/GetFastestNode',
            method: 'GET',
        }).success(function (data, status, headers, config) {
            $scope.fastestNode = data.Display;
            $scope.showFastestNode = true;
        });
    }
    $scope.saveTree = function () {
        $http({
            url: '/home/SaveTree',
            method: 'GET',
            params: { desc: $scope.descSave }
        }).success(function (data, status, headers, config) {


        });
    }
    $scope.init = function () {
        $http({
            url: '/home/LoadTreeList',
            method: 'GET',
        }).success(function (data, status, headers, config) {
            console.log(data);
            $scope.treeList = data;
        });
    }
    $scope.loadTreeFromDB = function () {
        $http({
            url: '/home/LoadTreeFromDB',
            method: 'GET',
            params: { Id: $scope.treeL }
        }).success(function (data, status, headers, config) {
            if (data != "") {
                $scope.isWorking = true;
                console.log("ok");
                $scope.GetTree();
                $scope.url = data;
                $scope.curAction = "once";
                $scope.getProgress();
                $scope.descSave = $scope.dbTree.Description;
                $scope.showMAft = true;
            }
        });
    }
    $scope.urlChanged = function () {
        $scope.showMAft = false;
    }
    var stopPromise;
    var param;
    $scope.getProgress = function () {
        $http({
            url: '/home/GetProgress',
            method: 'GET',
        }).success(function (data, status, headers, config) {
            $scope.setProgress(data);
            switch ($scope.curAction) {
                case "parse": {
                    $scope.pgbValue = $scope.Parsed;
                    if ($scope.Parsed == $scope.totalCount) {
                        $scope.showMAft = true;
                        $scope.GetTree();
                        $scope.isWorking = false;
                        $scope.curAction = ""
                        //$interval.cancel(stopPromise);
                    }
                    else {
                        if ($scope.realtime)
                            $scope.GetTree();
                        $timeout($scope.getProgress, 500);
                    }
                    break;
                }
                case "measure": {
                    $scope.pgbValue = $scope.Measured;
                    if ($scope.Measured == $scope.totalCount) {
                        $scope.GetTree();
                        $scope.isWorking = false;

                        $scope.curAction = ""
                        //$interval.cancel(stopPromise);
                    }
                    else {
                        if ($scope.realtime)
                            $scope.GetTree();
                        $timeout($scope.getProgress, 500);
                    }
                    break;
                }
                case "once": {
                    //$interval.cancel(stopPromise);
                    $scope.isWorking = false;
                    break;
                }

            }

        });
    }
    $scope.getInfoFromDb = function () {
        $http({
            url: '/home/GetTreeInfo',
            method: 'GET',
            params: { Id: $scope.treeL }
        }).success(function (data, status, headers, config) {
            console.log(data);
            $scope.ShowDbTreeInfo = true;
            $scope.dbTree.Date = data.Date;
            $scope.dbTree.Description = data.Description;
        });
    }
    $scope.startGettingProgress = function (param) {

        $scope.isWorking = true;
        $scope.getProgress();
        $timeout($scope.timeoutProgress, 30000);//ну просто архикостыль

        /*stopPromise = $interval(function () {
            $scope.getProgress();
            switch (param) {
                case "parse": {
                    console.log("in parse");
                    if ($scope.Parsed == $scope.totalCount) {
                        console.log("in parse if");
                        $scope.showMAft = true;
                        $scope.GetTree();
                        $scope.isWorking = false;
                        $interval.cancel(stopPromise);
                    }
                    break;
                }
                case "measure": {
                    console.log("in measure");
                    if ($scope.Measured == $scope.totalCount) {
                        console.log("in measure if");
                        $scope.GetTree();
                        $scope.isWorking = false;

                        $interval.cancel(stopPromise);
                    }
                    break;
                }
                case "once": {
                    console.log("in once");
                    $interval.cancel(stopPromise);
                    $scope.isWorking = false;
                    break;
                }
                default: {
                    $timeout($scope.timeoutProgress, 500);
                }

            }

        }, 500);*/
    }
    $scope.timeoutProgress = function () {
        $scope.getProgress();
        switch (param) {
            case "parse": {
                console.log("in parse");
                if ($scope.Parsed == $scope.totalCount) {
                    console.log("in parse if");
                    $scope.showMAft = true;
                    $scope.GetTree();
                    $scope.isWorking = false;
                    //$interval.cancel(stopPromise);
                }
                break;
            }
            case "measure": {
                console.log("in measure");
                if ($scope.Measured == $scope.totalCount) {
                    console.log("in measure if");
                    $scope.GetTree();
                    $scope.isWorking = false;

                    //$interval.cancel(stopPromise);
                }
                break;
            }
            case "once": {
                console.log("in once");
                //$interval.cancel(stopPromise);
                $scope.isWorking = false;
                break;
            }
            default: {
                $timeout($scope.timeoutProgress(param), 500);
            }

        }
    }

    $scope.GetTree = function () {
        $http({
            url: '/home/GetTree',
            method: 'GET',
        }).success(function (data, status, headers, config) {
            $scope.treeData = data;
        });

    }
    $scope.setProgress = function (data) {

        $scope.progressDiv = true;
        $scope.curParsing = data.CurrentlyParsing;
        $scope.curMeasuring = data.CurrentlyMeasuring;
        $scope.totalCount = data.Total;
        $scope.NotParsed = data.NotParsed;
        $scope.Parsed = data.Parsed;
        $scope.NotMeasured = data.NotMeasured;
        $scope.Measured = data.Measured;

    }


    $scope.checkOnlyDeeper = false;
    $scope.parseParams = false;
    $scope.curParsing = "";
    $scope.curMeasuring = "";
    $scope.totalCount = 0;
    $scope.NotParsed = 0;
    $scope.Parsed = 0;
    $scope.NotMeasured = 0;
    $scope.Measured = 0;
    $scope.progressDiv = false;
    $scope.isWorking = false;
    $scope.showMAft = false;
    $scope.prgParam = "";
    $scope.dbTree = {};
    $scope.dbTree.Date = ""
    $scope.dbTree.Description = "";
    $scope.ShowDbTreeInfo = false;
    $scope.slowestNode = "";
    $scope.showSlowestNode = false;
    $scope.fastestNode = "";
    $scope.showFastestNode = false;
    $scope.realtime = false;
    $scope.limit = 0;
    $scope.pgbValue = 0;



});