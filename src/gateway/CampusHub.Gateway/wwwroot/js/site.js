document.addEventListener("DOMContentLoaded", () => {
  document.querySelectorAll("table.js-datatable").forEach((table) => {
    const noSort = [];
    table.querySelectorAll("thead th").forEach((th, index) => {
      if (th.classList.contains("no-sort")) {
        noSort.push(index);
      }
    });

    new DataTable(table, {
      pageLength: 10,
      lengthMenu: [10, 25, 50],
      order: [],
      columnDefs: noSort.length ? [{ orderable: false, targets: noSort }] : [],
    });
  });
});
