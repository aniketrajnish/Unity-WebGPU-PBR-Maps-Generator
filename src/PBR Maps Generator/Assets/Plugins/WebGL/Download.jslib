mergeInto(LibraryManager.library, {
  jsDownloadImage: function(base64Data, fileName) {
    var extension = UTF8ToString(fileName).split('.').pop().toLowerCase();
    var mimeTypes = {
      'png': 'image/png',
      'jpg': 'image/jpeg',
      'jpeg': 'image/jpeg' 
    }; // only formats supported by Unity encoding
    var mimeType = mimeTypes[extension] || 'application/octet-stream';
    var dataUrl = `data:${mimeType};base64,` + UTF8ToString(base64Data);
    var link = document.createElement('a');
    link.download = UTF8ToString(fileName);
    link.href = dataUrl;
    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);
  }
});
