mergeInto(LibraryManager.library, {
  jsUploadImage: function () {
    var input = document.createElement('input');
    input.type = 'file';
    input.accept = '.png,.jpg,.jpeg'; // Only formats supported by Unity encoding
    input.onchange = e => { 
      var file = e.target.files[0]; 
      var reader = new FileReader();
      reader.readAsDataURL(file); 
      reader.onloadend = function () {
        var dataUrl = reader.result;
        // Concatenate dataUrl and file.name with a delimiter
        SendMessage('IO', 'LoadImage', dataUrl + '|' + file.name);
      }
    }
    input.click(); // Open file dialog
  }
});
