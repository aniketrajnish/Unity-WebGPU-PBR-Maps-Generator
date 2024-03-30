mergeInto(LibraryManager.library, {
  DownloadImage: function(base64Data, fileName) {
    var dataUrl = "data:image/png;base64," + Pointer_stringify(base64Data);
    var link = document.createElement('a');
    link.download = Pointer_stringify(fileName);
    link.href = dataUrl;
    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);
  }
});
