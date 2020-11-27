// Adds a "copy" button to code blocks

window.addEventListener("load", (_event) => {
    let blocks = document.body.querySelectorAll(".article pre code");
    for (let i = 0; i < blocks.length; i++) {
        let code = blocks.item(i);
        let contents = code.innerText
        let button = document.createElement("button");
        button.innerHTML = "Copy";
        button.classList.add("code__copy-button");
        button.onclick = () => {
            navigator.clipboard.writeText(contents);
        };
        code.insertAdjacentElement("beforebegin", button);
    }
});
