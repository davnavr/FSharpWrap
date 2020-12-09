// Adds color to code blocks using CSS classes used in highlight.js

window.addEventListener("load", (_event) => {
    let blocks = document.body.querySelectorAll("pre code");
    for (let i = 0; i < blocks.length; i++) {
        let pre = blocks.item(i);
        pre.classList.add("hljs");
    }
});
