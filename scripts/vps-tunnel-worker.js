const net = require("net");

function parseArgs(argv) {
  const args = {};
  for (let i = 0; i < argv.length; i += 1) {
    const key = argv[i];
    const value = argv[i + 1];
    if (!key.startsWith("--")) {
      continue;
    }

    args[key.slice(2)] = value;
    i += 1;
  }

  return args;
}

function required(args, name) {
  const value = args[name];
  if (!value) {
    throw new Error(`Missing required argument --${name}`);
  }

  return value;
}

const args = parseArgs(process.argv.slice(2));
const listenAddress = required(args, "listen-address");
const vpsHost = required(args, "vps-host");
const authPort = Number(required(args, "auth-port"));
const worldPort = Number(required(args, "world-port"));

function startForwarder(localPort, remotePort) {
  const server = net.createServer((sourceSocket) => {
    const targetSocket = net.createConnection({
      host: vpsHost,
      port: remotePort,
    });

    sourceSocket.pipe(targetSocket);
    targetSocket.pipe(sourceSocket);

    const closeSockets = () => {
      sourceSocket.destroy();
      targetSocket.destroy();
    };

    sourceSocket.on("error", closeSockets);
    targetSocket.on("error", closeSockets);
    sourceSocket.on("close", () => targetSocket.end());
    targetSocket.on("close", () => sourceSocket.end());
  });

  server.on("error", (error) => {
    console.error(`listen ${listenAddress}:${localPort} failed: ${error.message}`);
    process.exit(1);
  });

  server.listen(localPort, listenAddress);
  return server;
}

const servers = [
  startForwarder(authPort, authPort),
  startForwarder(worldPort, worldPort),
];

function shutdown(code) {
  let pending = servers.length;
  if (pending === 0) {
    process.exit(code);
    return;
  }

  for (const server of servers) {
    server.close(() => {
      pending -= 1;
      if (pending === 0) {
        process.exit(code);
      }
    });
  }
}

process.on("SIGINT", () => shutdown(0));
process.on("SIGTERM", () => shutdown(0));
