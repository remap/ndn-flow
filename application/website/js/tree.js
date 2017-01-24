/************************************************
 * D3 sync tree render function
 ************************************************/
var tree = [{
  "name": "/",
  "children": []
}]
var treeView = null;

function findAmongChildren(node, str) {
  for (var child in node["children"]) {
    if (node["children"][child]["name"] == str) {
      return node["children"][child];
    }
  }
  return null;
}

function insertToTree(data) {
  var dataName = data.getName();
  var idx = 0;
  var nameSize = dataName.size();

  var treeNode = tree[0];
  var child = {};
  var changed = false;

  do {
    child = findAmongChildren(treeNode, dataName.get(idx).toEscapedString());
    if (child == null) {
      break;
    }
    idx ++;
    treeNode = child;
  } while (child !== null && idx < nameSize);

  for (var j = idx; j < nameSize; j++) {
    var newChild = {
      "name": dataName.get(j).toEscapedString(),
      "children": []
    };
    treeNode["children"].push(newChild);
    treeNode = newChild;
    changed = true;
  }
  if (changed) {
    updateTree(tree);
    //console.log(tree);
  }
}

function updateTree() {
  treeView = new TreeView(tree, 'tree');
}
