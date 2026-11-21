// Lists every named definition in the old game's code with its exact line range (acorn parse), for og_extract.py.
//   node og_defs.js game.pretty.js > defs.json      (needs: npm i acorn acorn-walk)
const fs = require('fs'), acorn = require('acorn'), walk = require('acorn-walk');
const src = fs.readFileSync(process.argv[2], 'utf8');
const ast = acorn.parse(src, { ecmaVersion: 'latest', locations: true, allowReturnOutsideFunction: true });
const out = [];
const nameOf = n => n.type === 'Identifier' ? n.name : n.type === 'MemberExpression' && !n.computed ? (nameOf(n.object) ? nameOf(n.object) + '.' + n.property.name : null) : n.type === 'ThisExpression' ? 'this' : null;
const rng = n => [n.loc.start.line - 1, n.loc.end.line - 1];
function props(owner, obj) {
  if (!obj || obj.type !== 'ObjectExpression') return;
  for (const p of obj.properties) if (p.type === 'Property' && !p.computed && p.key && (p.key.name || p.key.value)) {
    const v = p.value, fn = /Function/.test(v.type);
    out.push({ o: owner, k: String(p.key.name || p.key.value), r: rng(p), f: fn ? 1 : 0 });
  }
}
walk.full(ast, n => {
  if (n.type === 'VariableDeclarator' && n.id.type === 'Identifier' && n.init) {
    out.push({ o: n.id.name, k: '', r: rng(n), f: /Function/.test(n.init.type) ? 1 : 0, top: 1 });
    let init = n.init;
    while (init && init.type === 'AssignmentExpression') init = init.right;
    props(n.id.name, init);
  } else if (n.type === 'AssignmentExpression' && n.left.type === 'MemberExpression') {
    const full = nameOf(n.left); if (!full) return;
    const parts = full.split('.'); if (parts[0] === 'window') parts.shift();
    if (parts.length === 1) { out.push({ o: parts[0], k: '', r: rng(n), f: /Function/.test(n.right.type) ? 1 : 0, top: 1 }); props(parts[0], n.right); }
    else if (parts.length >= 2) out.push({ o: parts[0], k: parts.slice(1).join('.'), r: rng(n), f: /Function/.test(n.right.type) ? 1 : 0 });
  } else if (n.type === 'FunctionDeclaration' && n.id) out.push({ o: '', k: n.id.name, r: rng(n), f: 1 });
});
process.stdout.write(JSON.stringify(out));
