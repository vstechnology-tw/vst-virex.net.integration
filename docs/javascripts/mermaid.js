const prepareMermaidBlocks = () => {
  const blocks = [];
  const sourceBlocks = Array.from(document.querySelectorAll("pre.mermaid:not([data-mermaid-loading])"));

  sourceBlocks.forEach((sourceBlock) => {
    const source = sourceBlock.textContent ?? "";
    const container = document.createElement("div");
    container.className = "mermaid";
    sourceBlock.replaceWith(container);
    blocks.push({ container, source });
  });

  return blocks;
};

const initialBlocks = prepareMermaidBlocks();

const createFallback = (source) => {
  const fallback = document.createElement("pre");
  fallback.className = "mermaid";
  const code = document.createElement("code");
  code.textContent = source;
  fallback.append(code);
  return fallback;
};

const restoreBlocks = (blocks) => {
  blocks.forEach(({ container, source }) => {
    if (container.isConnected) {
      container.replaceWith(createFallback(source));
    }
  });
};

import("https://cdn.jsdelivr.net/npm/mermaid@11.16.0/dist/mermaid.esm.min.mjs")
  .then(({ default: mermaid }) => {
    mermaid.initialize({
      startOnLoad: false,
      securityLevel: "strict",
      theme: "base",
      themeVariables: {
        primaryColor: "#e9f1f4",
        primaryTextColor: "#1f2933",
        primaryBorderColor: "#1f6078",
        lineColor: "#53636f",
        secondaryColor: "#fbfcfd",
        tertiaryColor: "#ffffff"
      }
    });

    const renderBlocks = (blocks) => {
      blocks.forEach(({ container, source }, index) => {
        void mermaid
          .render(`mermaid-diagram-${Date.now()}-${index}`, source)
          .then(({ svg, bindFunctions }) => {
            container.innerHTML = svg;
            bindFunctions?.(container);
          })
          .catch((error) => {
            container.replaceWith(createFallback(source));
            console.error("Mermaid rendering failed", error);
          });
      });
    };

    renderBlocks(initialBlocks);
    document$.subscribe(() => renderBlocks(prepareMermaidBlocks()));
  })
  .catch((error) => {
    restoreBlocks(initialBlocks);
    console.error("Mermaid initialization failed", error);
  });
