// lắng nghe sự kiện -> khi các element trong body thay đổi.
new MutationObserver((mutations, observer) => {
    // gọi hàm khi UI chuyển sang trạng thai -> reconnect-failed
    //reconnect-rejected
    if (
        document.querySelector(
            "#components-reconnect-modal.components-reconnect-failed .m1-custom-failed h1"
        ) ||
        document.querySelector(
            "#components-reconnect-modal.components-reconnect-rejected .m1-custom-failed h1"
        )
    ) {
        const Reconnection = async () => {
            await fetch("");
            location.reload(); // bắt reload lại
        };
        observer.disconnect();
        Reconnection();
        setInterval(Reconnection, 3500);
    }
}).observe(document.getElementById("components-reconnect-modal"), {
    attributes: true,
    characterData: true,
    childList: true,
    subtree: true,
    attributeOldValue: true,
    characterDataOldValue: true,
});