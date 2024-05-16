mergeInto(LibraryManager.library, {
  SendMessageToGiftNative: function(message) {
    parent.postMessage(Pointer_stringify(message), '*');
  },
});