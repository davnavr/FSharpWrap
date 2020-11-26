// Adds a "copy" button to code blocks

window.addEventListener("load", (_event) => {
    let labels = document.body.querySelectorAll(".article h2");
    for (let i = 0; i < labels.length; i++) {
        let h2 = labels.item(i);
        let name = h2.innerText;
        h2.id = name.toLowerCase().replaceAll(" ", "-");
    }
});
