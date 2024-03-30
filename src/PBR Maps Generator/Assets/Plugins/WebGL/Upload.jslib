mergeInto(LibraryManager.library, {
  UploadImage: function () {
    var input = document.createElement('input');
    input.type = 'file';
    input.accept = 'image/*'; // Restrict to image files
    input.onchange = e => { 
      var file = e.target.files[0]; 
      var reader = new FileReader();
      reader.readAsDataURL(file); 
      reader.onloadend = function () {
        var dataUrl = reader.result;
        SendMessage('IO', 'LoadImage', dataUrl);
      }
    }
    input.click(); // Open file dialog
  }
});