DownloadImage: function(base64Data, fileName) {
  var extension = fileName.split('.').pop().toLowerCase();
  var mimeTypes = {
    'png': 'image/png',
    'jpg': 'image/jpeg',
    'jpeg': 'image/jpeg'
  }; // only formats supported by unity encoding
  var mimeType = mimeTypes[extension] || 'application/octet-stream';
  var dataUrl = `data:${mimeType};base64,` + Pointer_stringify(base64Data);
  var link = document.createElement('a');
  link.download = Pointer_stringify(fileName);
  link.href = dataUrl;
  document.body.appendChild(link);
  link.click();
  document.body.removeChild(link);
}
