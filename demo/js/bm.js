var testdata = '[{"id":37,"name":"Alfredton Recreation Reserve","desc":"Playground  89 Cuthberts Rd  Alfredton","type":1,"typeString":"Playground","coords":{"lat":747341.4714446971,"long":5839918.852484712},"address":"89 Cuthberts Road, Alfredton, VIC, 3350","walktime":2,"biketime":1},{"id":42,"name":"Skate Structure","desc":"Skate Structure\\r\\nDoug Dean Reserve\\r\\nGreenhalghs Road\\r\\nDelacombe","type":2,"typeString":"Skate Park","coords":{"lat":143.81578899683387,"long":-37.58385047909834},"address":"Doug Dean Reserve, Greenhalghs Road, Delacombe, VIC, 3356","walktime":6,"biketime":3},{"id":2,"name":"Goodstart Early Learning Centre - Alfredton","desc":"91-93 Cutherberts Road, Alfredton, 3350","type":1,"typeString":"Child Care Centre","coords":{"lat":143.797761730033,"long":-37.553814619969},"address":"91-93 Cutherberts Road, Alfredton, 3350","walktime":2,"biketime":6}]';

var vm = null;
var options = {
  enableHighAccuracy: true,
  timeout: 5000,
  maximumAge: 0
};

// function success(pos) {
//   var crd = pos.coords;
//
//   console.log('Your current position is:');
//   console.log('Latitude : ' + crd.latitude);
//   console.log('Longitude: ' + crd.longitude);
//   console.log('More or less ' + crd.accuracy + ' meters.');
// };

function error(err) {
  console.warn('ERROR(' + err.code + '): ' + err.message);
};


//navigator.geolocation.getCurrentPosition(success, error, options);

$(document).ready(function() {
  if (vm == null) {
    vm = new CreateBMViewModel();
    ko.applyBindings(vm);
  }
    
})

function getResults(location, resultsObsv, transportMode) {
  $.ajax({
    url: 'http://ballaratminute.azurewebsites.net/',
    data: {
      lat: location.latitude,
      long: location.longitude
    }
  })
    .done(function(data) {
      //resultsObsv = ko.mapping.fromJS(data.d);
      ko.mapping.fromJSON(testdata, {}, resultsObsv);
      resultsObsv.sort(transportMode == "walk" ? walkSort : bikeSort);
    });
}

var walkSort = function(left, right) { return left.walktime() == right.walktime() ? 0 : (left.walktime() < right.walktime() ? -1 : 1) };
var bikeSort = function(left, right) { return left.biketime() == right.biketime() ? 0 : (left.biketime() < right.biketime() ? -1 : 1) };

function CreateBMViewModel() {
  var self = this;
  
  self.location = ko.observable();
  self.results = ko.observableArray();
  self.mode = ko.observable("walk");
  self.mode.subscribe(function(newValue) {
    self.results.sort(newValue == "walk" ? walkSort : bikeSort);
  });
  
  // self.displayTime = ko.computed(function(){
//     (self.mode() == "walk" ? walkSort : bikeSort)
//   });
  
  function success(pos) {
    self.location(pos.coords);
    getResults(self.location(), self.results, self.mode());

  };
  
  self.updateCoords = function() {
    navigator.geolocation.getCurrentPosition(success, error, options);
  }
}



